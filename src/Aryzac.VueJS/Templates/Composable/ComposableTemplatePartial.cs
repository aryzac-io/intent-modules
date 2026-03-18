using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.VueJS.Api;
using Intent.Engine;
using Intent.Metadata.ApiGateway.Api;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.Modules.Common.TypeScript.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TypeScript.Templates.TypescriptTemplatePartial", Version = "1.0")]

namespace Aryzac.VueJS.Templates.Composable
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    public partial class ComposableTemplate : TypeScriptTemplateBase<Aryzac.VueJS.Api.ComposableModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.VueJS.Composable";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public ComposableTemplate(IOutputTarget outputTarget, Aryzac.VueJS.Api.ComposableModel model) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(TemplateId);
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TypeScriptFileConfig(
                overwriteBehaviour: OverwriteBehaviour.Always,
                className: $"{Model.Name}",
                fileName: $"use{Model.Name.ToPascalCase()}");
        }

        public IEnumerable<ComposableServiceModel> GetServices()
        {
            return Model.InternalElement.ChildElements.Select(a => a.AsComposableServiceModel()).ToList();
        }

        public Route GetRoute(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            var responseType = endpoint.TypeReference.Element?.Name ?? "void";

            var method = apiGatewayRoute.GetUpstreamRouteInfo().Verb;
            var route = apiGatewayRoute.GetUpstreamRouteInfo().Route;

            var versioned = endpoint.GetStereotype("Api Version Settings");

            return new Route(
                Method: method.ToString().ToUpper(),
                Path: route.Replace("{", ":").Replace("}", ""),
                RequestType: endpoint.Name,
                ResponseType: responseType,
                Versioned: versioned != null
            );
        }

        public string GetScopes(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            var scopeSettings = endpoint.GetStereotype("59f81e19-6e8d-4da2-a8e6-ec72878d7f42");
            if (scopeSettings is null)
            {
                return "";
            }

            var scopesProperty = scopeSettings.GetProperty("Scopes");
            var verbProperty = scopeSettings.GetProperty("Verb");

            var scopesRaw = scopesProperty?.Value;
            var verbId = verbProperty?.Value;

            if (string.IsNullOrWhiteSpace(scopesRaw) || string.IsNullOrWhiteSpace(verbId))
            {
                return "";
            }

            // 1. Work out which application owns the endpoint
            var endpointAppId = endpoint.Package.ApplicationId; // adjust to your actual API

            // 2. Load the designers from that application
            var servicesDesigner = ExecutionContext.MetadataManager
                .GetDesigner(endpointAppId, "Services");
            var domainDesigner = ExecutionContext.MetadataManager
                .GetDesigner(endpointAppId, "Domain");

            // 3. Get all ScopeDefinitions and ScopeVerbs from that Services designer
            const string scopeDefinitionTypeId = "33256cb2-6ac1-48e5-b3dd-75c8e83f156e";
            const string scopeVerbTypeId = "561f871a-ac3f-4e82-b2ce-4cc129d7f264";
            const string scopeDefinitionSettingsStereoId = "effde114-12bd-498a-bf44-42f90df0e1ba";

            var scopeDefinitions = servicesDesigner.GetElementsOfType(scopeDefinitionTypeId).ToList();
            var verbs = servicesDesigner.GetElementsOfType(scopeVerbTypeId).ToList();

            var verb = verbs.FirstOrDefault(v => v.Id == verbId);
            if (verb is null)
            {
                return "";
            }

            // 4. Parse the list of scopeDefinition IDs from the property string
            var scopeIds = scopesRaw?
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Replace("\"", string.Empty).Trim())
                .ToList() ?? new List<string>();

            // 5. Filter scopeDefinitions to only the ones referenced from this endpoint
            var scopeDefinitionsForEndpoint = scopeDefinitions
                .Where(sd => scopeIds.Contains(sd.Id))
                .ToList();

            var result = new List<Scope>();

            foreach (var sd in scopeDefinitionsForEndpoint)
            {
                var sdSettings = sd.GetStereotype(scopeDefinitionSettingsStereoId);
                if (sdSettings is null)
                {
                    continue;
                }

                var boundedContextProp = sdSettings.GetProperty("Bounded Context");
                var resourceProp = sdSettings.GetProperty("Resource");
                var resourceNameProp = sdSettings.GetProperty("Resource Name");

                var boundedContext = boundedContextProp?.Value
                    ?? OutputTarget.ExecutionContext.GetApplicationConfig(endpointAppId).Name;

                var resourceId = resourceProp?.Value ?? string.Empty;
                var resource = domainDesigner.Elements.FirstOrDefault(e => e.Id == resourceId);
                if (resource is null)
                {
                    continue;
                }

                var resourceName = !string.IsNullOrWhiteSpace(resourceNameProp?.Value)
                    ? resourceNameProp.Value
                    : resource.Name;

                var scopeName =
                    $"{boundedContext.ToKebabCase()}.{resourceName.ToKebabCase()}.{verb.Name.ToKebabCase()}";

                result.Add(new Scope(scopeName));
            }

            return string.Join(",", result.Select(s => $"'{s.Name}'"));
        }

        public IElement GetRouteResponseType(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            return (IElement)endpoint.TypeReference.Element;
        }

        public IElement GetRouteRequestType(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            return (IElement)endpoint;
        }
        public string AddImport(IElement dtoElement)
        {
            if (dtoElement.ParentElement is null)
                return "";

            var parentElementPath = dtoElement.ParentElement.Name.ToPascalCase();

            var type =
                dtoElement.IsCommandModel() ? "command" :
                dtoElement.IsQueryModel() ? "query" :
                "dto";

            var filename = $"{dtoElement.Name.ToKebabCase().RemoveSuffix($"-{type}")}.{type}";

            return $"import type {{ {dtoElement.Name} }} from \"../types/dto/{parentElementPath}/{filename}\";";
        }

    }

    [IntentManaged(Mode.Ignore)]
    public record Route(string Method, string Path, string RequestType, string ResponseType, bool Versioned);

    [IntentManaged(Mode.Ignore)]
    public record Scope(string Name);
}