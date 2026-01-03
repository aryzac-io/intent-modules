using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiMetadataProviderExtensions", Version = "1.0")]

namespace Aryzac.VueJS.Api
{
    public static class ApiMetadataProviderExtensions
    {
        public static IList<ComposableModel> GetComposableModels(this IDesigner designer)
        {
            return designer.GetElementsOfType(ComposableModel.SpecializationTypeId)
                .Select(x => new ComposableModel(x))
                .ToList();
        }

        public static IList<ComposableServiceModel> GetComposableServiceModels(this IDesigner designer)
        {
            return designer.GetElementsOfType(ComposableServiceModel.SpecializationTypeId)
                .Select(x => new ComposableServiceModel(x))
                .ToList();
        }

    }
}