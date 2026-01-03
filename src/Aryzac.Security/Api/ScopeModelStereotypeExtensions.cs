using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModelExtensions", Version = "1.0")]

namespace Aryzac.Security.Api
{
    public static class ScopeModelStereotypeExtensions
    {
        public static ScopeSettings GetScopeSettings(this ScopeModel model)
        {
            var stereotype = model.GetStereotype(ScopeSettings.DefinitionId);
            return stereotype != null ? new ScopeSettings(stereotype) : null;
        }


        public static bool HasScopeSettings(this ScopeModel model)
        {
            return model.HasStereotype(ScopeSettings.DefinitionId);
        }

        public static bool TryGetScopeSettings(this ScopeModel model, out ScopeSettings stereotype)
        {
            if (!HasScopeSettings(model))
            {
                stereotype = null;
                return false;
            }

            stereotype = new ScopeSettings(model.GetStereotype(ScopeSettings.DefinitionId));
            return true;
        }

        public class ScopeSettings
        {
            private IStereotype _stereotype;
            public const string DefinitionId = "effde114-12bd-498a-bf44-42f90df0e1ba";

            public ScopeSettings(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

            public string BoundedContext()
            {
                return _stereotype.GetProperty<string>("Bounded Context");
            }

            public IElement Resource()
            {
                return _stereotype.GetProperty<IElement>("Resource");
            }

            public string ResourceName()
            {
                return _stereotype.GetProperty<string>("Resource Name");
            }

        }

    }
}