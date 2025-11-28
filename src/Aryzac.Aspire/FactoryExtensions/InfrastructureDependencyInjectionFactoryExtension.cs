using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Nuget;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.CSharp.VisualStudio;
using Intent.Modules.Common.Plugins;
using Intent.Modules.Common.Templates;
using Intent.Modules.EntityFrameworkCore;
using Intent.Modules.EntityFrameworkCore.Settings;
using Intent.Modules.Metadata.RDBMS.Settings;
using Intent.Plugins.FactoryExtensions;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.FactoryExtension", Version = "1.0")]

namespace Aryzac.Aspire.FactoryExtensions
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public class InfrastructureDependencyInjectionFactoryExtension : FactoryExtensionBase
    {
        public override string Id => "Aryzac.Aspire.InfrastructureDependencyInjectionFactoryExtension";

        [IntentManaged(Mode.Ignore)]
        public override int Order => 1000;

        /// <summary>
        /// This is an example override which would extend the
        /// <see cref="ExecutionLifeCycleSteps.AfterTemplateRegistrations"/> phase of the Software Factory execution.
        /// See <see cref="FactoryExtensionBase"/> for all available overrides.
        /// </summary>
        /// <remarks>
        /// It is safe to update or delete this method.
        /// </remarks>
        protected override void OnAfterTemplateRegistrations(IApplication application)
        {
            var dependencyInjectionTemplate = application.FindTemplateInstance<ICSharpFileBuilderTemplate>("Infrastructure.DependencyInjection");
            if (dependencyInjectionTemplate is null)
            {
                return;
            }

            var dbContexts = DbContextManager.GetDbContexts(application.Id, application.MetadataManager);
            ApplyConfigurationStatements(dependencyInjectionTemplate, dbContexts);
        }

        private static void ApplyConfigurationStatements(ICSharpFileBuilderTemplate dependencyInjectionTemplate, IEnumerable<DbContextInstance> dbContexts)
        {
            dependencyInjectionTemplate.CSharpFile.OnBuild(file =>
            {
                file.AddUsing("Microsoft.EntityFrameworkCore");
                file.AddUsing("Microsoft.Azure.Cosmos");
                var method = file.Classes.First().FindMethod("AddInfrastructure");
                foreach (var dbContextInstance in dbContexts)
                {
                    // Matching the options.UseCosmos (or other database provider) by matching the connection string
                    // ⚠️ PROBABLY BETTER TO MARK the options.UsePROVIDER statement with specific metadata to clear 
                    //    up this logic.
                    var dbContextConnectionStringStatement = method.FindStatement(s => s.HasMetadata("is-connection-string"));
                    var newStatement = UpdateAddDbContextStatement(
                        dependencyInjectionTemplate,
                        dbContextInstance,
                        dbContextConnectionStringStatement.Parent,
                        dependencyInjectionTemplate.ExecutionContext);

                    method.FindAndReplaceStatement(s => s == dbContextConnectionStringStatement.Parent, newStatement);
                }
            });
        }

        // No need for this module at all if this is embedded in Intent.Modules.EntityFrameworkCore. 
        // In case this stays a custom module, we can extend this to support more providers as needed.
        private static CSharpInvocationStatement UpdateAddDbContextStatement(
            ICSharpFileBuilderTemplate dependencyInjection,
            DbContextInstance dbContextInstance,
            IHasCSharpStatementsActual optionsStatement,
            ISoftwareFactoryExecutionContext executionContext)
        {
            CSharpInvocationStatement statement = null;

            var targetDbProvider = DbContextManager.GetDatabaseProviderForDbContext(dbContextInstance.DbProvider, executionContext);
            switch (targetDbProvider)
            {
                case DatabaseSettingsExtensions.DatabaseProviderOptionsEnum.InMemory:
                    break;
                case DatabaseSettingsExtensions.DatabaseProviderOptionsEnum.SqlLite:
                    break;

                case DatabaseSettingsExtensions.DatabaseProviderOptionsEnum.SqlServer:
                    break;

                case DatabaseSettingsExtensions.DatabaseProviderOptionsEnum.Postgresql:
                    break;

                case DatabaseSettingsExtensions.DatabaseProviderOptionsEnum.MySql:
                    break;

                case DatabaseSettingsExtensions.DatabaseProviderOptionsEnum.Cosmos:
                    // Cosmos requires some additional configuration when using the emulator.
                    // This needs to be disabled for production deployments, should maybe be 
                    // driven via a setting or wrapped up in a #if DEBUG directive.

                    statement = new CSharpInvocationStatement("options.UseCosmos")
                        .WithArgumentsOnNewLines()
                        .AddArgument(
                            @"configuration[""ConnectionStrings:cosmos-db""]",
                            a => a.AddMetadata("is-connection-string", true))
                        .AddArgument(@"configuration[""Cosmos:DatabaseName""]")
                        .AddArgument(
                            new CSharpLambdaBlock("cosmosOptions")
                                .AddStatement("#if DEBUG")
                                .AddStatement("// Required for emulator")
                                .AddStatement(
                                    new CSharpInvocationStatement("cosmosOptions.HttpClientFactory")
                                        .AddArgument(
                                            new CSharpLambdaBlock("()")
                                                .AddStatement("var httpMessageHandler = new HttpClientHandler();")
                                                .AddStatement(
                                                    "httpMessageHandler.ServerCertificateCustomValidationCallback = " +
                                                    "HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;")
                                                .AddReturn(new CSharpStatement("new HttpClient(httpMessageHandler)"))
                                        )
                                )
                                .AddStatement("cosmosOptions.ConnectionMode(ConnectionMode.Gateway);")
                                .AddStatement("cosmosOptions.LimitToEndpoint();")
                                .AddStatement("#endif")
                        );

                    break;

                case DatabaseSettingsExtensions.DatabaseProviderOptionsEnum.Oracle:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, "Database Provider has not been set to a valid value. Please fix in the Database Settings.");
            }

            return statement;
        }

        /// <summary>
        /// This is an example override which would extend the
        /// <see cref="ExecutionLifeCycleSteps.BeforeTemplateExecution"/> phase of the Software Factory execution.
        /// See <see cref="FactoryExtensionBase"/> for all available overrides.
        /// </summary>
        /// <remarks>
        /// It is safe to update or delete this method.
        /// </remarks>
        protected override void OnBeforeTemplateExecution(IApplication application)
        {
            // Your custom logic here.
        }
    }
}