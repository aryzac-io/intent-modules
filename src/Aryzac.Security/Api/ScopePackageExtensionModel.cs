using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiPackageExtensionModel", Version = "1.0")]

namespace Aryzac.Security.Api
{
    [IntentManaged(Mode.Merge)]
    public class ScopePackageExtensionModel : ServicesPackageModel
    {
        [IntentManaged(Mode.Ignore)]
        public ScopePackageExtensionModel(IPackage package) : base(package)
        {
        }

        [IntentManaged(Mode.Fully)]
        public ScopeConfigurationModel ScopeConfiguration => UnderlyingPackage.ChildElements
            .GetElementsOfType(ScopeConfigurationModel.SpecializationTypeId)
            .Select(x => new ScopeConfigurationModel(x))
            .SingleOrDefault();

    }
}