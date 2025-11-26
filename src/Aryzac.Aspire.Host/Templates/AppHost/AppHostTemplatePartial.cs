using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aryzac.Aspire.Host;
using Intent.Configuration;
using Intent.Engine;
using Intent.IArchitect.Agent.Persistence;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.AppStartup;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Aspire.Host.Templates.AppHost
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class AppHostTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Aspire.Host.AppHost";
        public readonly IApplicationConfig[] apps;

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public AppHostTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            const string aspireModule = "Aryzac.Aspire";

            apps = ExecutionContext.GetSolutionConfig()
                .GetApplicationReferences()
                .Select(app => ExecutionContext.GetSolutionConfig().GetApplicationConfig(app.Id))
                .Where(app => app.Modules.Any(x => x.ModuleId == aspireModule))
                .ToArray();

            var builderCreationStatement = new CSharpStatement("var builder = DistributedApplication.CreateBuilder(args);");
            var addServicesComment = new CSharpStatement("// Add services to the container.");
            var builderRunStatement = new CSharpStatement("builder.Build().Run();");

            CSharpFile = new CSharpFile(string.Empty, this.GetFolderPath())
                .AddTopLevelStatements(config =>
                {
                    config.AddStatement(builderCreationStatement, s =>
                    {
                        s.AddMetadata("is-builder-statement", true);
                        s.SeparatedFromPrevious();
                    });

                    config.AddStatement(addServicesComment, s =>
                    {
                        s.AddMetadata("is-add-services-to-container-comment", true);
                        s.SeparatedFromPrevious();
                    });

                    config.AddStatement(builderRunStatement, s =>
                    {
                        s.AddMetadata("host-run", true);
                        s.SeparatedFromPrevious();
                    });
                });

            // Add global application services such as Application Insights, Azure Keyvault, databases, etc
            AddApplicationServices(CSharpFile);

            // Add builder projects
            AddServiceDefinitions(CSharpFile);

            // Add application specific configuration
            foreach (var app in apps)
            {
                AddApplicationConfiguration(CSharpFile, app);
            }

            // Cleanup unused comments
            CSharpFile.AfterBuild(file =>
            {
                //file.TopLevelStatements.RemoveStatement(addServicesComment);
            });
        }

        private void AddApplicationServices(CSharpFile cSharpFile)
        {
            AddApplicationInsights(cSharpFile);
            AddCosmosDB(cSharpFile);
        }

        private void AddApplicationInsights(CSharpFile cSharpFile)
        {
            bool hasOpenTelemetry = HasApplicationInsights();

            if (hasOpenTelemetry)
            {
                cSharpFile.AfterBuild(config =>
                {
                    var statements = config.TopLevelStatements;

                    var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                    var applicationInsightsComment = new CSharpStatement("// Provision Application Insights resource");

                    lastStatement.InsertAbove(applicationInsightsComment, s =>
                    {
                        s.SeparatedFromPrevious();
                        s.AddMetadata("is-application-insights-comment", true);
                    });

                    lastStatement = applicationInsightsComment;

                    var appInsightsRegistrationStatement = new CSharpStatement("var insights = builder.AddAzureApplicationInsights(\"application-insights\");");

                    lastStatement.InsertBelow(appInsightsRegistrationStatement, s =>
                    {
                        s.AddMetadata("is-application-insights", true);
                    });
                });
            }
        }

        private void AddCosmosDB(CSharpFile cSharpFile)
        {
            bool hasCosmosDb = HasCosmosDb();

            if (hasCosmosDb)
            {
                cSharpFile.AfterBuild(config =>
                {
                    var statements = config.TopLevelStatements;

                    var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                    var applicationInsightsComment = new CSharpStatement("// Provision Cosmos Db resource");

                    lastStatement.InsertAbove(applicationInsightsComment, s =>
                    {
                        s.SeparatedFromPrevious();
                        s.AddMetadata("is-cosmos-db-comment", true);
                    });

                    lastStatement = applicationInsightsComment;

                    var cosmosDbRegistrationStatements = new List<CSharpStatement>
                    {
                        new CSharpStatement("#pragma warning disable ASPIRECOSMOSDB001"),
                        new CSharpStatement("var cosmos = builder.AddAzureCosmosDB(\"cosmos-db\")"),
                        new CSharpStatement("    .RunAsPreviewEmulator("),
                        new CSharpStatement("        emulator =>"),
                        new CSharpStatement("        {"),
                        new CSharpStatement("            emulator.WithDataExplorer());"),
                        new CSharpStatement("            emulator.WithLifetime(ContainerLifetime.Persistent));"),
                        new CSharpStatement("        })"),
                        new CSharpStatement(";"),
                        new CSharpStatement("#pragma warning restore ASPIRECOSMOSDB001"),
                        new CSharpStatement(),
                        new CSharpStatement("var cosmosConnectionString = new ConnectionStringReference(cosmos.Resource, optional: false);")
                    };

                    lastStatement.InsertBelow(cosmosDbRegistrationStatements.ToArray());
                });
            }
        }

        private void AddServiceDefinitions(CSharpFile cSharpFile)
        {
            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;

                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var serviceDefinitionsComment = new CSharpStatement("// Service Definitions");

                lastStatement.InsertAbove(serviceDefinitionsComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-service-definitions-comment", true);
                });

                lastStatement = serviceDefinitionsComment;

                foreach (var app in apps)
                {
                    var appNameParts = app.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    var varAppName = string.Join("", appNameParts.Select(m => m.ToPascalCase())).ToCamelCase();
                    var projectReferenceAppName = string.Join("_", appNameParts.Select(m => m.ToPascalCase()));

                    var appServiceRegistrationStatement = new CSharpStatement($"var {varAppName}Api = builder.AddProject<Projects.{projectReferenceAppName}_Api>(\"{app.Name.ToKebabCase()}\");");
                    lastStatement.InsertBelow(appServiceRegistrationStatement);

                    lastStatement = appServiceRegistrationStatement;
                }
            });
        }

        private void AddApplicationConfiguration(CSharpFile cSharpFile, IApplicationConfig app)
        {
            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;

                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var serviceDefinitionsComment = new CSharpStatement($"// {app.Name} Configuration");

                lastStatement.InsertAbove(serviceDefinitionsComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-app-configuration-comment", true);
                    s.AddMetadata("application", app.Id);
                });

                lastStatement = serviceDefinitionsComment;

                var appNameParts = app.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var varAppName = string.Join("", appNameParts.Select(m => m.ToPascalCase())).ToCamelCase();

                var serviceStatements = new List<CSharpStatement>();

                if (HasCosmosDb(app))
                {
                    serviceStatements.Add(new CSharpStatement($"var {varAppName}DB = cosmos.AddCosmosDatabase(\"{varAppName.ToKebabCase()}-db\", \"{app.Name}DB\");"));
                }

                serviceStatements.Add(new CSharpStatement($"{varAppName}Api"));

                if (HasApplicationInsights())
                {
                    serviceStatements.Add(new CSharpStatement($"    .WithReference(insights)"));
                }
                if (HasCosmosDb(app))
                {
                    serviceStatements.Add(new CSharpStatement($"    .WithReference({varAppName}DB)"));
                    serviceStatements.Add(new CSharpStatement($"    .WaitFor({varAppName}DB)"));
                }

                serviceStatements.Add(new CSharpStatement($";"));

                lastStatement.InsertBelow(serviceStatements.ToArray());
            });
        }

        private bool HasApplicationInsights()
        {
            return HasModule("Intent.OpenTelemetry");
        }

        private bool HasCosmosDb()
        {
            return IntegrationManager.Instance.HasCosmosDbProvider();
        }

        private bool HasCosmosDb(IApplicationConfig app)
        {
            return IntegrationManager.Instance.HasCosmosDbProvider(app);
        }

        private bool HasModule(string ModuleId)
        {
            foreach (var app in apps)
            {
                var module = app.Modules.FirstOrDefault(m => m.ModuleId == ModuleId);
                if (module != null)
                {
                    return true;
                }
            }

            return false;
        }

        [IntentManaged(Mode.Fully)]
        public CSharpFile CSharpFile { get; }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        protected override CSharpFileConfig DefineFileConfig()
        {
            return new CSharpFileConfig(
                className: $"AppHost",
                @namespace: $"{this.GetNamespace()}",
                relativeLocation: $"{this.GetFolderPath()}",
                fileName: "AppHost");
        }

        [IntentManaged(Mode.Fully)]
        public override string TransformText()
        {
            return CSharpFile.ToString();
        }
    }
}