using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiMetadataProviderExtensions", Version = "1.0")]

namespace Aryzac.Security.Api
{
    public static class ApiMetadataProviderExtensions
    {
        public static IList<ScopeModel> GetScopeModels(this IDesigner designer)
        {
            return designer.GetElementsOfType(ScopeModel.SpecializationTypeId)
                .Select(x => new ScopeModel(x))
                .ToList();
        }

        public static IList<ScopeConfigurationModel> GetScopeConfigurationModels(this IDesigner designer)
        {
            return designer.GetElementsOfType(ScopeConfigurationModel.SpecializationTypeId)
                .Select(x => new ScopeConfigurationModel(x))
                .ToList();
        }

        public static IList<ScopeVerbModel> GetScopeVerbModels(this IDesigner designer)
        {
            return designer.GetElementsOfType(ScopeVerbModel.SpecializationTypeId)
                .Select(x => new ScopeVerbModel(x))
                .ToList();
        }

    }
}