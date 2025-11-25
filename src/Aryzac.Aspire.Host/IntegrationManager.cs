using Intent.Configuration;
using Intent.Engine;
using Intent.EntityFrameworkCore.Api;
using Intent.Modelers.Domain.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aryzac.Aspire.Host;

internal class IntegrationManager
{
    private static IntegrationManager? _instance;
    public static void Initialize(ISoftwareFactoryExecutionContext executionContext)
    {
        _instance = new IntegrationManager(executionContext);
    }

    public static IntegrationManager Instance
    {
        get
        {
            if (_instance is null)
            {
                throw new InvalidOperationException("Module Manager not initialized.");
            }

            return _instance;
        }
    }

    private readonly List<CosmosDbDatabaseProvider> _databaseProviders;

    private IntegrationManager(ISoftwareFactoryExecutionContext executionContext)
    {
        var applications = executionContext.GetSolutionConfig()
            .GetApplicationReferences()
            .Select(app => executionContext.GetSolutionConfig().GetApplicationConfig(app.Id))
            .ToArray();

        const string domainModelModule = "Intent.Modelers.Domain";

        _databaseProviders = applications
            .Where(app => app.Modules.Any(x => x.ModuleId == domainModelModule))
            .Select(app =>
                new CosmosDbDatabaseProvider(
                    app.Id,
                    app.Name,
                    executionContext.MetadataManager.Domain(app.Id)
                        .GetClassModels()
                        .Any(c => c.HasCosmosDBContainerSettings())
                )
            )
            .Distinct()
            .ToList();
    }

    public bool HasCosmosDbProvider()
    {
        return _databaseProviders
            .Any(p => p.HasCosmosDbProvider);
    }

    public bool HasCosmosDbProvider(IApplicationConfig app)
    {
        return _databaseProviders
            .Where(a => a.ApplicationId == app.Id)
            .Any(p => p.HasCosmosDbProvider);
    }

    private record CosmosDbDatabaseProvider(string ApplicationId, string ApplicationName, bool HasCosmosDbProvider);
}