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
                var method = file.Classes.First().FindMethod("AddInfrastructure");
                foreach (var dbContextInstance in dbContexts)
                {
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

        private static CSharpStatement UpdateAddDbContextStatement(
            ICSharpFileBuilderTemplate dependencyInjection,
            DbContextInstance dbContextInstance,
            IHasCSharpStatementsActual optionsStatement,
            ISoftwareFactoryExecutionContext executionContext)
        {
            var statement = new CSharpStatementBlock();

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
                    var cosmosOptionsStatements = new List<CSharpStatement>
                    {
                        new CSharpStatement("// Required for emulator"),
                        new CSharpStatement("cosmosOptions.HttpClientFactory("),
                        new CSharpStatement("            () =>"),
                        new CSharpStatement("            {"),
                        new CSharpStatement("                var httpMessageHandler = new HttpClientHandler();"),
                        new CSharpStatement("                httpMessageHandler.ServerCertificateCustomValidationCallback ="),
                        new CSharpStatement("                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;"),
                        new CSharpStatement("                return new HttpClient(httpMessageHandler);"),
                        new CSharpStatement("            });"),
                        new CSharpStatement("        cosmosOptions.ConnectionMode(ConnectionMode.Gateway);"),
                        new CSharpStatement("        cosmosOptions.LimitToEndpoint();")
                    };

                    statement.AddStatement(new CSharpInvocationStatement("options.UseCosmos")
                        .WithArgumentsOnNewLines()
                        .AddArgument(@"configuration[""ConnectionStrings:cosmos-db""]", a => a.AddMetadata("is-connection-string", true))
                        .AddArgument(@"configuration[""Cosmos:DatabaseName""]")
                        .AddLambdaBlock(@"cosmosOptions", block =>
                        {
                            block.AddStatements(cosmosOptionsStatements);
                        })
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