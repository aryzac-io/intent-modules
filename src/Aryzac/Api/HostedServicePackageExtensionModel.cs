using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiPackageExtensionModel", Version = "1.0")]

namespace Aryzac.Api
{
    [IntentManaged(Mode.Merge)]
    public class HostedServicePackageExtensionModel : ServicesPackageModel
    {
        [IntentManaged(Mode.Ignore)]
        public HostedServicePackageExtensionModel(IPackage package) : base(package)
        {
        }

        [IntentManaged(Mode.Fully)]
        public IList<HostedServiceModel> HostedServices => UnderlyingPackage.ChildElements
            .GetElementsOfType(HostedServiceModel.SpecializationTypeId)
            .Select(x => new HostedServiceModel(x))
            .ToList();

    }
}