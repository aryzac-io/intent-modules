using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aryzac.Aspire.Host;
using Intent.Configuration;
using Intent.Engine;
using Intent.IArchitect.Agent.Persistence;
using Intent.Metadata.ApiGateway.Api;
using Intent.Modelers.Services.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.AppStartup;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.Modules.Common.VisualStudio;
using Intent.Modules.EntityFrameworkCore;
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
                // TODO: Need to add projects as a dependency on this csproj
                AddApplicationConfiguration(CSharpFile, app);
            }

            // Add builder projects
            AddYarp(CSharpFile);

            // Finalize with local run overrides
            AddLocalRunOverrides(CSharpFile);

            // Cleanup unused comments
            CSharpFile.AfterBuild(file =>
            {
                file.TopLevelStatements.RemoveStatement(addServicesComment);
            });
        }

        private void AddApplicationServices(CSharpFile cSharpFile)
        {
            AddApplicationInsights(cSharpFile);
            AddAzureKeyVault(cSharpFile);
            AddRabbitMq(cSharpFile);
            AddCosmosDB(cSharpFile);
            AddAzureBlobStorage(cSharpFile);
        }

        public static readonly INugetPackageInfo AspireHostingAzureApplicationInsights = new NugetPackageInfo("Aspire.Hosting.Azure.ApplicationInsights", "13.1.0");
        public static readonly INugetPackageInfo AspireHostingAzureCosmosDB = new NugetPackageInfo("Aspire.Hosting.Azure.CosmosDB", "13.1.0");
        public static readonly INugetPackageInfo AspireHostingAzureStorage = new NugetPackageInfo("Aspire.Hosting.Azure.Storage", "13.1.0");
        public static readonly INugetPackageInfo AspireHostingYarp = new NugetPackageInfo("Aspire.Hosting.Yarp", "13.1.0");
        public static readonly INugetPackageInfo AspireHostingAzureKeyVault = new NugetPackageInfo("Aspire.Hosting.Azure.KeyVault", "13.1.0");
        public static readonly INugetPackageInfo AspireHostingRabbitMQ = new NugetPackageInfo("Aspire.Hosting.RabbitMQ", "13.1.0");


        private void AddApplicationInsights(CSharpFile cSharpFile)
        {
            if (!HasApplicationInsights())
            {
                return;
            }

            AddNugetDependency(AspireHostingAzureApplicationInsights);

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

                var appInsightsRegistrationStatement = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration($"insights"),
                    new CSharpStatement($"builder.AddAzureApplicationInsights(\"application-insights\");")
                );

                lastStatement.InsertBelow(appInsightsRegistrationStatement, s =>
                {
                    s.AddMetadata("is-application-insights", true);
                });

                lastStatement = appInsightsRegistrationStatement;

                var applicationInsightsConnectionString = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration($"applicationInsightsConnectionString"),
                    new CSharpStatement($"new ConnectionStringReference(insights.Resource, optional: false);")
                );

                lastStatement.InsertBelow(applicationInsightsConnectionString, stmt =>
                {
                    stmt.SeparatedFromPrevious();
                });
            });
        }

        private void AddAzureKeyVault(CSharpFile cSharpFile)
        {
            if (!HasKeyVault())
            {
                return;
            }

            AddNugetDependency(AspireHostingAzureKeyVault);

            // Needed for KeyVaultBuiltInRole and kv.Resource.UriExpression usage
            AddUsing("Azure.Provisioning.KeyVault");

            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;
                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var keyVaultComment = new CSharpStatement("// Provision Azure Key Vault resource (shared)");

                lastStatement.InsertAbove(keyVaultComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-keyvault-comment", true);
                });

                lastStatement = keyVaultComment;

                var kvStatement = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("kv"),
                    new CSharpStatement("builder.AddAzureKeyVault(\"shared-kv\");")
                );

                lastStatement.InsertBelow(kvStatement, s => s.AddMetadata("is-keyvault", true));
                lastStatement = kvStatement;

                var commonComment = new CSharpStatement("// Common Key Vault settings");
                lastStatement.InsertBelow(commonComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-keyvault-common-comment", true);
                });
                lastStatement = commonComment;

                var keyVaultEnabled = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("keyVaultEnabled"),
                    new CSharpStatement("builder.ExecutionContext.IsRunMode ? \"false\" : \"true\";")
                );

                lastStatement.InsertBelow(keyVaultEnabled);
                lastStatement = keyVaultEnabled;

                var keyVaultEndpointExpr = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("keyVaultEndpointExpr"),
                    new CSharpStatement("kv.Resource.UriExpression;")
                );

                lastStatement.InsertBelow(keyVaultEndpointExpr, s => s.SeparatedFromPrevious());
            });
        }

        private void AddRabbitMq(CSharpFile cSharpFile)
        {
            if (!HasRabbitMqServiceBus())
            {
                return;
            }

            AddNugetDependency(AspireHostingRabbitMQ);

            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;
                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var rabbitComment = new CSharpStatement("// Provision RabbitMQ resource");

                lastStatement.InsertAbove(rabbitComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-rabbitmq-comment", true);
                });

                lastStatement = rabbitComment;

                // var rabbitMq = builder.AddRabbitMQ("messaging")
                //     .WithManagementPlugin();
                CSharpStatement rabbitMqStatement = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("rabbitMq"),
                    new CSharpStatement("builder.AddRabbitMQ(\"messaging\")")
                );

                rabbitMqStatement = rabbitMqStatement.AddInvocation("WithManagementPlugin", cfg =>
                {
                    cfg.OnNewLine();
                });

                lastStatement.InsertBelow(rabbitMqStatement, s => s.AddMetadata("is-rabbitmq", true));
                lastStatement = rabbitMqStatement;

                var rabbitMqHostString = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("rabbitMqHostString"),
                    new CSharpStatement("rabbitMq.Resource.Host;")
                );

                lastStatement.InsertBelow(rabbitMqHostString);
                lastStatement = rabbitMqHostString;

                var rabbitMqPortString = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("rabbitMqPortString"),
                    new CSharpStatement("rabbitMq.Resource.Port;")
                );

                lastStatement.InsertBelow(rabbitMqPortString);
                lastStatement = rabbitMqPortString;

                var rabbitMqPasswordString = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("rabbitMqPasswordString"),
                    new CSharpStatement("rabbitMq.Resource.PasswordParameter;")
                );

                lastStatement.InsertBelow(rabbitMqPasswordString, s => s.SeparatedFromPrevious());
            });
        }

        private void AddAzureBlobStorage(CSharpFile cSharpFile)
        {
            if (!HasBlobStorage())
            {
                return;
            }

            AddNugetDependency(AspireHostingAzureStorage);

            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;

                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var azureStorageComment = new CSharpStatement("// Provision Azure Storage resource");

                lastStatement.InsertAbove(azureStorageComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-azure-storage-comment", true);
                });

                lastStatement = azureStorageComment;

                CSharpStatement addAzureStorageStatement = new CSharpAssignmentStatement(
                        new CSharpVariableDeclaration($"azureStorage"),
                        new CSharpStatement($"builder.AddAzureStorage(\"storage\")")
                    );

                addAzureStorageStatement = addAzureStorageStatement.AddInvocation("RunAsEmulator", config =>
                {
                    config.OnNewLine();
                    config.AddArgument(new CSharpLambdaBlock("azurite")
                        .AddStatement("azurite.WithLifetime(ContainerLifetime.Persistent);")
                        .AddStatement("azurite.WithDataVolume();"));
                });

                lastStatement.InsertBelow([addAzureStorageStatement]);
            });
        }

        private void AddCosmosDB(CSharpFile cSharpFile)
        {
            if (!HasCosmosDb())
            {
                return;
            }

            AddNugetDependency(AspireHostingAzureCosmosDB);
            AddUsing("Azure.Provisioning.CosmosDB");
            AddUsing("System.Linq"); // for Single()

            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;
                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var cosmosDbComment = new CSharpStatement("// Provision Cosmos Db resource");
                lastStatement.InsertAbove(cosmosDbComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-cosmos-db-comment", true);
                });
                lastStatement = cosmosDbComment;

                CSharpStatement addAzureCosmosDbStatement = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("cosmos"),
                    new CSharpStatement("builder.AddAzureCosmosDB(\"cosmos-db\")")
                );

                if (HasKeyVault())
                {
                    addAzureCosmosDbStatement = addAzureCosmosDbStatement.AddInvocation("WithAccessKeyAuthentication", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument("kv");
                    });
                }

                addAzureCosmosDbStatement = addAzureCosmosDbStatement.AddInvocation("ConfigureInfrastructure", cfg =>
                {
                    cfg.OnNewLine();

                    var infraLambda = new CSharpLambdaBlock("infra")
                        .AddStatement("var cosmosDbAccount = infra.GetProvisionableResources()")
                        .AddStatement("    .OfType<CosmosDBAccount>()")
                        .AddStatement("    .Single();")
                        .AddStatement(string.Empty)
                        .AddStatement("cosmosDbAccount.DisableLocalAuth = false;")
                        .AddStatement(string.Empty)
                        .AddStatement("cosmosDbAccount.BackupPolicy = new ContinuousModeBackupPolicy()")
                        .AddStatement("{")
                        .AddStatement("    ContinuousModeTier = ContinuousTier.Continuous7Days")
                        .AddStatement("};");

                    cfg.AddArgument(infraLambda);
                });

                addAzureCosmosDbStatement = addAzureCosmosDbStatement.AddInvocation("RunAsPreviewEmulator", cfg =>
                {
                    cfg.OnNewLine();
                    cfg.AddArgument(new CSharpLambdaBlock("emulator")
                        .AddStatement("emulator.WithDataExplorer();")
                        .AddStatement("emulator.WithLifetime(ContainerLifetime.Persistent);")
                        .AddStatement("emulator.WithDataVolume();"));
                });

                var pragmaDisabled = new CSharpStatement("#pragma warning disable ASPIRECOSMOSDB001");
                var pragmaRestored = new CSharpStatement("#pragma warning restore ASPIRECOSMOSDB001");

                lastStatement.InsertBelow([pragmaDisabled, addAzureCosmosDbStatement, pragmaRestored]);
                lastStatement = pragmaRestored;

                // non Key Vault mode: emit cosmosConnectionString (original way)
                if (!HasKeyVault())
                {
                    var cosmosConnectionString = new CSharpAssignmentStatement(
                        new CSharpVariableDeclaration("cosmosConnectionString"),
                        new CSharpStatement("new ConnectionStringReference(cosmos.Resource, optional: false);")
                    );

                    lastStatement.InsertBelow(cosmosConnectionString, stmt => stmt.SeparatedFromPrevious());
                    lastStatement = cosmosConnectionString;
                }
            });
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

                    var appServiceRegistrationStatement = new CSharpAssignmentStatement(
                        new CSharpVariableDeclaration($"{varAppName}Api"),
                        new CSharpStatement($"builder.AddProject<Projects.{projectReferenceAppName}_Api>(\"{FormatProjectName(app.Name)}\");")
                    );

                    lastStatement.InsertBelow(appServiceRegistrationStatement);

                    lastStatement = appServiceRegistrationStatement;
                }
            });
        }

        private static string FormatProjectName(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return string.Empty;
            }

            var kebab = Name.ToKebabCase();

            // Remove anything that isn't an ASCII letter, digit, or hyphen
            return System.Text.RegularExpressions.Regex.Replace(kebab, "[^A-Za-z0-9-]", "");
        }

        private void AddApplicationConfiguration(CSharpFile cSharpFile, IApplicationConfig app)
        {
            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;

                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var serviceDefinitionsComment = new CSharpStatement($"// {app.Name} configuration");

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
                    var cosmosDbStatement = new CSharpAssignmentStatement(
                        new CSharpVariableDeclaration($"{varAppName}DB"),
                        new CSharpStatement($"cosmos.AddCosmosDatabase(\"{varAppName.ToKebabCase()}-db\", \"{app.Name}DB\");"));

                    lastStatement.InsertBelow(cosmosDbStatement, stmt => { stmt.SeparatedFromNext(); });

                    lastStatement = cosmosDbStatement;
                }

                if (HasBlobStorage(app))
                {
                    var blobStorageStatement = new CSharpAssignmentStatement(
                        new CSharpVariableDeclaration($"{varAppName}Storage"),
                        new CSharpStatement($"azureStorage.AddBlobContainer(\"{varAppName.ToKebabCase()}-blobs\");"));

                    lastStatement.InsertBelow(blobStorageStatement);

                    lastStatement = blobStorageStatement;

                    var azureStorageConnectionString = new CSharpAssignmentStatement(
                        new CSharpVariableDeclaration($"{varAppName}StorageConnectionString"),
                        new CSharpStatement($"new ConnectionStringReference({varAppName}Storage.Resource, optional: false);")
                    );

                    lastStatement.InsertBelow(azureStorageConnectionString, stmt =>
                    {
                        stmt.SeparatedFromNext();
                    });

                    lastStatement = azureStorageConnectionString;
                }

                var appApiStatement = new CSharpStatement($"{varAppName}Api");

                appApiStatement = appApiStatement.AddInvocation("WithReferenceEnvironment", config =>
                {
                    config.OnNewLine();
                    config.AddArgument("ReferenceEnvironmentInjectionFlags.None");
                });

                if (HasApplicationInsights())
                {
                    appApiStatement = appApiStatement.AddInvocation("WithEnvironment", config =>
                    {
                        config.OnNewLine();
                        config.AddArgument($"\"ApplicationInsights__ConnectionString\"");
                        config.AddArgument($"applicationInsightsConnectionString");
                    });
                    appApiStatement = appApiStatement.AddInvocation("WithReference", config =>
                    {
                        config.OnNewLine();
                        config.AddArgument("insights");
                    });
                }
                if (HasKeyVault())
                {
                    appApiStatement = appApiStatement.AddInvocation("WithEnvironment", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument("\"KeyVault__Enabled\"");
                        cfg.AddArgument("keyVaultEnabled");
                    });

                    appApiStatement = appApiStatement.AddInvocation("WithEnvironment", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument("\"KeyVault__Endpoint\"");
                        cfg.AddArgument("keyVaultEndpointExpr");
                    });

                    appApiStatement = appApiStatement.AddInvocation("WithRoleAssignments", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument("kv");
                        cfg.AddArgument("KeyVaultBuiltInRole.KeyVaultReader");
                        cfg.AddArgument("KeyVaultBuiltInRole.KeyVaultSecretsUser");
                    });
                }
                if (HasRabbitMqServiceBus())
                {
                    appApiStatement = appApiStatement.AddInvocation("WithEnvironment", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument("name", "\"RabbitMq__Host\"");
                        cfg.AddArgument("value", "rabbitMqHostString");
                    });

                    appApiStatement = appApiStatement.AddInvocation("WithEnvironment", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument("name", "\"RabbitMq__Port\"");
                        cfg.AddArgument("value", "rabbitMqPortString");
                    });

                    appApiStatement = appApiStatement.AddInvocation("WithEnvironment", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument("name", "\"RabbitMq__Password\"");
                        cfg.AddArgument("value", "rabbitMqPasswordString");
                    });

                    appApiStatement = appApiStatement.AddInvocation("WithReference", config =>
                    {
                        config.OnNewLine();
                        config.AddArgument($"rabbitMq");
                    });

                    appApiStatement = appApiStatement.AddInvocation("WaitFor", config =>
                    {
                        config.OnNewLine();
                        config.AddArgument($"rabbitMq");
                    });
                }
                if (HasCosmosDb(app))
                {
                    if (!HasKeyVault())
                    {
                        appApiStatement = appApiStatement.AddInvocation("WithEnvironment", cfg =>
                        {
                            cfg.OnNewLine();
                            cfg.AddArgument("\"Cosmos__ConnectionString\"");
                            cfg.AddArgument("cosmosConnectionString");
                        });

                        appApiStatement = appApiStatement.AddInvocation("WithReference", cfg =>
                        {
                            cfg.OnNewLine();
                            cfg.AddArgument($"{varAppName}DB");
                        });
                    }

                    appApiStatement = appApiStatement.AddInvocation("WaitFor", cfg =>
                    {
                        cfg.OnNewLine();
                        cfg.AddArgument($"{varAppName}DB");
                    });
                }

                if (HasBlobStorage(app))
                {
                    appApiStatement = appApiStatement.AddInvocation("WithEnvironment", config =>
                    {
                        config.OnNewLine();
                        config.AddArgument($"\"AzureBlobStorage\"");
                        config.AddArgument($"{varAppName}StorageConnectionString");
                    });
                    appApiStatement = appApiStatement.AddInvocation("WithReference", config =>
                    {
                        config.OnNewLine();
                        config.AddArgument($"{varAppName}Storage");
                    });
                    appApiStatement = appApiStatement.AddInvocation("WaitFor", config =>
                    {
                        config.OnNewLine();
                        config.AddArgument($"{varAppName}Storage");
                    });
                }
                if (HasServiceProxy(app))
                {
                    foreach (var targetApp in apps.Where(a => a.Id != app.Id))
                    {
                        if (!HasServiceProxies(OutputTarget, app, targetApp))
                        {
                            continue;
                        }

                        var targetAppNameParts = targetApp.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                        var varTargetAppName = string.Join("", targetAppNameParts.Select(m => m.ToPascalCase())).ToCamelCase();

                        var serviceProxyPackageNames = GetServiceProxyPackageNames(OutputTarget, app, targetApp);

                        foreach (var serviceProxyPackageName in serviceProxyPackageNames)
                        {
                            appApiStatement = appApiStatement.AddInvocation("WithEnvironment", config =>
                            {
                                config.OnNewLine();
                                config.AddArgument($"\"HttpClients__{serviceProxyPackageName}__Uri\"");
                                config.AddArgument($"{varTargetAppName}Api.GetEndpoint(\"https\")");
                            });
                        }
                        appApiStatement = appApiStatement.AddInvocation("WithReference", config =>
                        {
                            config.OnNewLine();
                            config.AddArgument($"{varTargetAppName}Api");
                        });
                        appApiStatement = appApiStatement.AddInvocation("WaitFor", config =>
                        {
                            config.OnNewLine();
                            config.AddArgument($"{varTargetAppName}Api");
                        });
                    }
                }

                lastStatement.InsertBelow(appApiStatement, stmt => { stmt.SeparatedFromNext(); });

                lastStatement = appApiStatement;
            });
        }

        private void AddYarp(CSharpFile cSharpFile)
        {
            if (!HasApiGateway())
            {
                return;
            }

            AddNugetDependency(AspireHostingYarp);

            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;

                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                var serviceDefinitionsComment = new CSharpStatement("// Application Gateway (YARP) configuration");

                lastStatement.InsertAbove(serviceDefinitionsComment, s =>
                {
                    s.SeparatedFromPrevious();
                    s.AddMetadata("is-service-definitions-comment", true);
                });

                lastStatement = serviceDefinitionsComment;

                var apiGatewayRoutes = GetApiGatewayRoutes(OutputTarget);

                // Build yarp configuration lambda first (unchanged logic, just moved up)
                var yarpLambdaConfigurationStatement = new CSharpLambdaBlock("yarp");
                var lastApplicationName = string.Empty;
                var lastPackageName = string.Empty;

                foreach (var apiGatewayRoute in apiGatewayRoutes
                    .OrderBy(r => r.DownstreamEndpoints().FirstOrDefault()?.Package?.ApplicationId ?? string.Empty)
                    .ThenBy(r => r.DownstreamEndpoints().FirstOrDefault()?.Package?.Name ?? string.Empty))
                {
                    var route = apiGatewayRoute.GetUpstreamRouteInfo().Route;
                    var package = apiGatewayRoute.DownstreamEndpoints()[0].Package;
                    var method = apiGatewayRoute.GetUpstreamRouteInfo().Verb;
                    var serviceApplicationId = package.ApplicationId;
                    var serviceApplication = apps.FirstOrDefault(app => app.Id == serviceApplicationId);

                    if (serviceApplication == null)
                    {
                        throw new Exception($"Unable to find application with ID '{serviceApplicationId}' for API Gateway route '{route}'");
                    }

                    var appNameParts = serviceApplication.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    var varAppName = string.Join("", appNameParts.Select(m => m.ToPascalCase())).ToCamelCase();

                    if (lastApplicationName != serviceApplication.Name)
                    {
                        yarpLambdaConfigurationStatement.AddStatement($"// Routes to {serviceApplication.Name} ({package.Name})", stmt =>
                        {
                            if (lastApplicationName != string.Empty)
                            {
                                stmt.SeparatedFromPrevious();
                            }
                        });

                        yarpLambdaConfigurationStatement.AddStatement(
                            new CSharpAssignmentStatement(
                                new CSharpVariableDeclaration($"{varAppName}Cluster"),
                                new CSharpInvocationStatement("yarp.AddCluster").AddArgument("resource", $"{varAppName}Api")
                            )
                        );

                        lastApplicationName = serviceApplication.Name;
                        lastPackageName = package.Name;
                    }
                    else if (lastPackageName != package.Name)
                    {
                        yarpLambdaConfigurationStatement.AddStatement($"// Routes to package {package.Name}");
                        lastPackageName = package.Name;
                    }

                    yarpLambdaConfigurationStatement.AddStatement($"yarp.AddRoute(\"{route}\", {varAppName}Cluster).WithMatchMethods(\"{method.ToString().ToUpper()}\");");
                }

                // Create gateway with chained method calls
                CSharpStatement gatewayStatement = new CSharpAssignmentStatement(
                    new CSharpVariableDeclaration("gateway"),
                    new CSharpStatement("builder.AddYarp(\"gateway\")")
                );

                gatewayStatement = gatewayStatement.AddInvocation("WithConfiguration", cfg =>
                {
                    cfg.OnNewLine();
                    cfg.AddArgument(yarpLambdaConfigurationStatement);
                });

                gatewayStatement = gatewayStatement.AddInvocation("WithExternalHttpEndpoints", cfg =>
                {
                    cfg.OnNewLine();
                });

                lastStatement.InsertBelow(gatewayStatement);
            });
        }

        private void AddLocalRunOverrides(CSharpFile cSharpFile)
        {
            // Only relevant if Cosmos exists at all
            if (!HasCosmosDb())
            {
                return;
            }

            cSharpFile.AfterBuild(config =>
            {
                var statements = config.TopLevelStatements;

                var lastStatement = statements.FindStatement(s => s.HasMetadata("is-add-services-to-container-comment"));

                // Only add if at least one app actually uses Cosmos
                var cosmosApps = apps.Where(HasCosmosDb).ToArray();
                if (cosmosApps.Length == 0)
                {
                    return;
                }

                // if (builder.ExecutionContext.IsRunMode) { ... }
                var ifBlock = new CSharpIfStatement("builder.ExecutionContext.IsRunMode");
                
                ifBlock.AddStatement($"// LOCAL RUN ONLY: feed emulator connection string via ConnectionStrings__cosmos-db so your code can use GetConnectionString(\"cosmos-db\")");

                foreach (var app in cosmosApps)
                {
                    var appNameParts = app.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    var varAppName = string.Join("", appNameParts.Select(m => m.ToPascalCase())).ToCamelCase();

                    ifBlock.AddStatement($"{varAppName}Api.WithEnvironment(\"ConnectionStrings__cosmos-db\", cosmos.Resource.ConnectionStringExpression);");
                }

                lastStatement.InsertBelow(ifBlock);
            });
        }

        private const string apiGatewayRouteTypeId = "b09d7684-5dde-4d4b-9cb5-0707bfd8578f";

        private IEnumerable<ApiGatewayRouteModel> GetApiGatewayRoutes(IOutputTarget outputTarget)
        {
            var servicesDesigner = outputTarget.ExecutionContext.MetadataManager.GetDesigner(outputTarget.ExecutionContext.GetApplicationConfig().Id, "Services");

            if (servicesDesigner == null)
            {
                return Enumerable.Empty<ApiGatewayRouteModel>();
            }

            return servicesDesigner.GetElementsOfType(apiGatewayRouteTypeId).Select(m => m.AsApiGatewayRouteModel());
        }

        private bool HasServiceProxies(IOutputTarget outputTarget, IApplicationConfig source, IApplicationConfig target)
        {
            var servicesDesigner = outputTarget.ExecutionContext.MetadataManager.GetDesigner(source.Id, "Services");

            if (servicesDesigner == null)
            {
                return false;
            }

            var serviceProxies = servicesDesigner.GetAssociationsOfType("3e69085c-fa2f-44bd-93eb-41075fd472f8");

            return serviceProxies.Any(sp => sp.TargetEnd.TypeReference.Element.Package.ApplicationId == target.Id);
        }

        private IEnumerable<string> GetServiceProxyPackageNames(IOutputTarget outputTarget, IApplicationConfig source, IApplicationConfig target)
        {
            var servicesDesigner = outputTarget.ExecutionContext.MetadataManager.GetDesigner(source.Id, "Services");

            if (servicesDesigner == null)
            {
                return [];
            }

            var serviceProxies = servicesDesigner.GetAssociationsOfType("3e69085c-fa2f-44bd-93eb-41075fd472f8");

            return serviceProxies
                .Where(sp => sp.TargetEnd.TypeReference.Element.Package.ApplicationId == target.Id)
                .Select(sp => sp.TargetEnd.TypeReference.Element.Package.Name);
        }

        private bool HasApplicationInsights()
        {
            return HasModule("Intent.OpenTelemetry");
        }

        private bool HasBlobStorage()
        {
            return HasModule("Intent.Azure.BlobStorage");
        }

        private bool HasKeyVault()
        {
            return HasModule("Intent.Azure.KeyVault");
        }

        private const string MassTransitModuleSettingsId = "b1c11f3f-63ce-4917-8ffb-b6c7698346c7";
        private const string MassTransitModuleSettingsMessageServiceProviderId = "2888b373-0419-4d33-ba56-2d8d0bf98eb9";
        private const string MassTransitModuleSettingsMessageServiceProviderRabbitMqValue = "rabbitmq";

        private bool HasRabbitMqServiceBus()
        {
            var apps = ExecutionContext.GetSolutionConfig()
                .GetApplicationReferences()
                .Select(app => ExecutionContext.GetSolutionConfig().GetApplicationConfig(app.Id))
                .ToArray();

            var hasRabbitMqServiceBus = apps.Any(app =>
            {
                var massTransitModuleSettings = app.ModuleSetting.FirstOrDefault(ms => ms.Id == MassTransitModuleSettingsId);

                if (massTransitModuleSettings == null)
                {
                    return false;
                }

                var messageServiceProvider = massTransitModuleSettings.GetSetting(MassTransitModuleSettingsMessageServiceProviderId);

                return messageServiceProvider.Value == MassTransitModuleSettingsMessageServiceProviderRabbitMqValue;
            });

            return hasRabbitMqServiceBus;
        }

        private bool HasBlobStorage(IApplicationConfig app)
        {
            return app.Modules.FirstOrDefault(m => m.ModuleId == "Intent.Azure.BlobStorage") != null;
        }

        private bool HasApiGateway()
        {
            return OutputTarget.ExecutionContext.InstalledModules.FirstOrDefault(m => m.ModuleId == "Intent.Metadata.ApiGateway") != null;
        }

        private bool HasServiceProxy(IApplicationConfig app)
        {
            return app.Modules.FirstOrDefault(m => m.ModuleId == "Intent.Modelers.Types.ServiceProxies") != null;
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