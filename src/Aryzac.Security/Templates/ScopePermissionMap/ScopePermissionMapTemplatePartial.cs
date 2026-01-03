using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Security.Templates.ScopePermissionMap
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class ScopePermissionMapTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.ScopePermissionMap";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public ScopePermissionMapTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddRecord($"Scope", record =>
                {
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Name");
                        ctor.AddParameter("List<Type>", "Permissions");
                    });

                    record.AddMethod("string", "ToString", method =>
                    {
                        method.Override();
                        method.AddStatement("return $\"{Name}\";");
                    });
                })
                .AddClass($"Scopes", @class =>
                {
                    var scopeConstants = GetScopeConstants(outputTarget);

                    @class.AddField("List<Scope>", "_scopes", field => {
                        field.WithAssignment(new CSharpStatement("new List<Scope>()"));
                    });
                    @class.AddProperty("IReadOnlyList<Scope>", "All", prop =>
                    {
                        prop.Getter.WithExpressionImplementation("_scopes");
                        prop.WithoutSetter();
                    });

                    @class.AddConstructor(ctor =>
                    {
                        foreach (var scopeConstant in scopeConstants)
                        {
                            var permissions = $"[{string.Join(", ", scopeConstant.Permissions.Select(m => $"typeof({m})"))}]";
                            var resourceName = string.IsNullOrWhiteSpace(scopeConstant.ResourceName) ? scopeConstant.Resource.Name.ToKebabCase() : scopeConstant.ResourceName;
                            ctor.AddStatement($"_scopes.Add(new Scope(Name: \"{scopeConstant.BoundedContext.ToKebabCase()}.{resourceName}.{scopeConstant.Verb.ToKebabCase()}\", Permissions: {permissions}));");
                        }
                    });
                });
        }

        private IEnumerable<ScopeConstant> GetScopeConstants(IOutputTarget outputTarget)
        {
            List<ScopeConstant> securityModels = [];

            var servicesDesigner = outputTarget.ExecutionContext.MetadataManager.GetDesigner(outputTarget.ExecutionContext.GetApplicationConfig().Id, "Services");
            var domainDesigner = outputTarget.ExecutionContext.MetadataManager.GetDesigner(outputTarget.ExecutionContext.GetApplicationConfig().Id, "Domain");

            securityModels.AddRange(GetDesignerScopeConstants(servicesDesigner, domainDesigner, outputTarget));

            return securityModels;
        }

        private const string scopeDefinitionTypeId = "33256cb2-6ac1-48e5-b3dd-75c8e83f156e";
        private const string scopeVerbTypeId = "561f871a-ac3f-4e82-b2ce-4cc129d7f264";
        private const string commandTypeId = "ccf14eb6-3a55-4d81-b5b9-d27311c70cb9";
        private const string queryTypeId = "e71b0662-e29d-4db2-868b-8a12464b25d0";
        private const string classTypeId = "04e12b51-ed12-42a3-9667-a6aa81bb6d10";

        private IEnumerable<ScopeConstant> GetDesignerScopeConstants(
            IDesigner designer,
            IDesigner domainDesigner,
            IOutputTarget outputTarget)
        {
            var scopeDefinitions = designer.GetElementsOfType(scopeDefinitionTypeId);
            var scopeVerbs = designer.GetElementsOfType(scopeVerbTypeId);
            var commands = designer.GetElementsOfType(commandTypeId);
            var queries = designer.GetElementsOfType(queryTypeId);

            var commandTemplates = outputTarget.ExecutionContext
                .FindTemplateInstances<IIntentTemplate<CommandModel>>("Intent.Application.MediatR.CommandModels")
                .ToArray();

            var queryTemplates = outputTarget.ExecutionContext
                .FindTemplateInstances<IIntentTemplate<QueryModel>>("Intent.Application.MediatR.QueryModels")
                .ToArray();

            var scopeDefinitionConstants = new List<ScopeDefinitionConstant>();

            foreach (var scopeDefinition in scopeDefinitions)
            {
                var scopeDefinitionSettings = scopeDefinition.GetStereotype("effde114-12bd-498a-bf44-42f90df0e1ba");

                var boundedContextProperty = scopeDefinitionSettings?.GetProperty("Bounded Context");
                var resourceProperty = scopeDefinitionSettings?.GetProperty("Resource");
                var resourceNameProperty = scopeDefinitionSettings?.GetProperty("Resource Name");

                var boundedContext = boundedContextProperty?.Value ?? outputTarget.ExecutionContext.GetApplicationConfig().Name;
                var resourceId = resourceProperty?.Value ?? string.Empty;

                var resource = domainDesigner.Elements.FirstOrDefault(e => e.Id == resourceId);
                if (resource is null)
                {
                    continue;
                }

                scopeDefinitionConstants.Add(new ScopeDefinitionConstant(
                    Id: scopeDefinition.Id,
                    Name: scopeDefinition.Name.ToPascalCase(),
                    BoundedContext: boundedContext,
                    ResourceId: resourceId,
                    Resource: resource,
                    ResourceName: string.IsNullOrWhiteSpace(resourceNameProperty?.Value) ? null : resourceNameProperty!.Value,
                    ResourceType: (resource.SpecializationTypeId == classTypeId)
                        ? ResourceType.Class
                        : ResourceType.DomainService));
            }

            // key: unique per (BoundedContext, ResourceId, VerbId)
            var scopesByKey = new Dictionary<string, ScopeConstant>();

            void AddPermission(
                string permissionName,
                ScopeDefinitionConstant scopeDefinition,
                IElement verb)
            {
                // You can choose any stable key you like; this is just one option
                var key = $"{scopeDefinition.BoundedContext}|{scopeDefinition.ResourceId}|{verb.Id}";

                if (!scopesByKey.TryGetValue(key, out var scopeConstant))
                {
                    scopeConstant = new ScopeConstant(
                        Name: scopeDefinition.Name,
                        BoundedContext: scopeDefinition.BoundedContext,
                        ResourceId: scopeDefinition.ResourceId,
                        Resource: scopeDefinition.Resource,
                        ResourceName: scopeDefinition.ResourceName,
                        ResourceType: scopeDefinition.ResourceType,
                        Verb: verb.Name,
                        Permissions: new List<string>());

                    scopesByKey.Add(key, scopeConstant);
                }

                // List<string> is mutable, so you can safely add to it
                if (!scopeConstant.Permissions.Contains(permissionName))
                {
                    scopeConstant.Permissions.Add(permissionName);
                }
            }

            // Commands -> scopes
            foreach (var commandTemplate in commandTemplates)
            {
                commandTemplate.TryGetModel(out CommandModel? command);

                if (command == null)
                {
                    continue;
                }

                var scopeSettings = command.GetStereotype("59f81e19-6e8d-4da2-a8e6-ec72878d7f42");
                if (scopeSettings is null)
                {
                    continue;
                }

                var scopesProperty = scopeSettings.GetProperty("Scopes");
                var verbProperty = scopeSettings.GetProperty("Verb");

                var scopesRaw = scopesProperty?.Value;
                var verbId = verbProperty?.Value;
                var verb = scopeVerbs.FirstOrDefault(v => v.Id == verbId);
                if (verb is null)
                {
                    continue;
                }

                var scopeIds = scopesRaw?
                        .Replace("[", "")
                        .Replace("]", "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Replace("\"", "").Trim())
                        .ToList()
                    ?? new List<string>();

                var scopeDefinitionsForCommand = scopeDefinitionConstants
                    .Where(sd => scopeIds.Contains(sd.Id))
                    .ToList();

                foreach (var scopeDefinition in scopeDefinitionsForCommand)
                {
                    AddPermission($"{commandTemplate.GetNamespace()}.{command.Name.Replace("Command", "")}.{command.Name}", scopeDefinition, verb);
                }
            }

            // Queries -> scopes
            foreach (var queryTemplate in queryTemplates)
            {
                queryTemplate.TryGetModel(out QueryModel? query);

                if (query == null)
                {
                    continue;
                }

                var scopeSettings = query.GetStereotype("59f81e19-6e8d-4da2-a8e6-ec72878d7f42");
                if (scopeSettings is null)
                {
                    continue;
                }

                var scopesProperty = scopeSettings.GetProperty("Scopes");
                var verbProperty = scopeSettings.GetProperty("Verb");

                var scopesRaw = scopesProperty?.Value;
                var verbId = verbProperty?.Value;
                var verb = scopeVerbs.FirstOrDefault(v => v.Id == verbId);
                if (verb is null)
                {
                    continue;
                }

                var scopeIds = scopesRaw?
                        .Replace("[", "")
                        .Replace("]", "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Replace("\"", "").Trim())
                        .ToList()
                    ?? new List<string>();

                var scopeDefinitionsForQuery = scopeDefinitionConstants
                    .Where(sd => scopeIds.Contains(sd.Id))
                    .ToList();

                foreach (var scopeDefinition in scopeDefinitionsForQuery)
                {
                    AddPermission($"{queryTemplate.GetNamespace()}.{query.Name.Replace("Query", "")}.{query.Name}", scopeDefinition, verb);
                }
            }

            // Return the aggregated scopes, each with all its permissions populated
            return scopesByKey.Values;
        }

        [IntentManaged(Mode.Fully)]
        public CSharpFile CSharpFile { get; }

        [IntentManaged(Mode.Fully)]
        protected override CSharpFileConfig DefineFileConfig()
        {
            return CSharpFile.GetConfig();
        }

        [IntentManaged(Mode.Fully)]
        public override string TransformText()
        {
            return CSharpFile.ToString();
        }

        private record ScopeDefinitionConstant(string Id, string Name, string BoundedContext, string ResourceId, IElement? Resource, string? ResourceName, ResourceType ResourceType);
        private record ScopeConstant(string Name, string BoundedContext, string ResourceId, IElement? Resource, string? ResourceName, ResourceType ResourceType, string Verb, List<string> Permissions)
        {
            public override string ToString()
            {
                return $"{BoundedContext.ToKebabCase()}.{ResourceName ?? Resource.Name.ToKebabCase()}.{Verb.ToKebabCase()}";
            }
        }

        private enum ResourceType
        {
            Class,
            DomainService
        }
    }
}