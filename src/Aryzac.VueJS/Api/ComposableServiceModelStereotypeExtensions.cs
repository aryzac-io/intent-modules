using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModelExtensions", Version = "1.0")]

namespace Aryzac.VueJS.Api
{
    public static class ComposableServiceModelStereotypeExtensions
    {
        public static ServiceSettings GetServiceSettings(this ComposableServiceModel model)
        {
            var stereotype = model.GetStereotype(ServiceSettings.DefinitionId);
            return stereotype != null ? new ServiceSettings(stereotype) : null;
        }


        public static bool HasServiceSettings(this ComposableServiceModel model)
        {
            return model.HasStereotype(ServiceSettings.DefinitionId);
        }

        public static bool TryGetServiceSettings(this ComposableServiceModel model, out ServiceSettings stereotype)
        {
            if (!HasServiceSettings(model))
            {
                stereotype = null;
                return false;
            }

            stereotype = new ServiceSettings(model.GetStereotype(ServiceSettings.DefinitionId));
            return true;
        }

        public class ServiceSettings
        {
            private IStereotype _stereotype;
            public const string DefinitionId = "37cd75f4-fdb3-4af2-9d0c-a73ef0ddd0b5";

            public ServiceSettings(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

            public IElement Service()
            {
                return _stereotype.GetProperty<IElement>("Service");
            }

        }

    }
}