using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModelExtensions", Version = "1.0")]

namespace Aryzac.Security.Api
{
    public static class QueryModelStereotypeExtensions
    {
        public static ScopeSettings GetScopeSettings(this QueryModel model)
        {
            var stereotype = model.GetStereotype(ScopeSettings.DefinitionId);
            return stereotype != null ? new ScopeSettings(stereotype) : null;
        }

        public static IReadOnlyCollection<ScopeSettings> GetScopeSettingss(this QueryModel model)
        {
            var stereotypes = model
                .GetStereotypes(ScopeSettings.DefinitionId)
                .Select(stereotype => new ScopeSettings(stereotype))
                .ToArray();

            return stereotypes;
        }


        public static bool HasScopeSettings(this QueryModel model)
        {
            return model.HasStereotype(ScopeSettings.DefinitionId);
        }

        public static bool TryGetScopeSettings(this QueryModel model, out ScopeSettings stereotype)
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
            public const string DefinitionId = "59f81e19-6e8d-4da2-a8e6-ec72878d7f42";

            public ScopeSettings(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

            public IElement[] Scopes()
            {
                return _stereotype.GetProperty<IElement[]>("Scopes") ?? new IElement[0];
            }

            public IElement Verb()
            {
                return _stereotype.GetProperty<IElement>("Verb");
            }

        }

    }
}