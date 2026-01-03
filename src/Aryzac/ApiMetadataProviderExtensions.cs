using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiMetadataProviderExtensions", Version = "1.0")]

namespace Aryzac.Api
{
    public static class ApiMetadataProviderExtensions
    {
        public static IList<HostedServiceModel> GetHostedServiceModels(this IDesigner designer)
        {
            return designer.GetElementsOfType(HostedServiceModel.SpecializationTypeId)
                .Select(x => new HostedServiceModel(x))
                .ToList();
        }

    }
}