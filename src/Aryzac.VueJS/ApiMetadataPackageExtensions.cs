using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiMetadataPackageExtensions", Version = "1.0")]

namespace Aryzac.VueJS.Api
{
    public static class ApiMetadataPackageExtensions
    {
        public static IList<ClientModel> GetClientModels(this IDesigner designer)
        {
            return designer.GetPackagesOfType(ClientModel.SpecializationTypeId)
                .Select(x => new ClientModel(x))
                .ToList();
        }

        public static bool IsClientModel(this IPackage package)
        {
            return package?.SpecializationTypeId == ClientModel.SpecializationTypeId;
        }


    }
}