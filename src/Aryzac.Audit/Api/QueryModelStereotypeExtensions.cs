using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModelExtensions", Version = "1.0")]

namespace Aryzac.Audit.Api
{
    public static class QueryModelStereotypeExtensions
    {
        public static Audit GetAudit(this QueryModel model)
        {
            var stereotype = model.GetStereotype(Audit.DefinitionId);
            return stereotype != null ? new Audit(stereotype) : null;
        }


        public static bool HasAudit(this QueryModel model)
        {
            return model.HasStereotype(Audit.DefinitionId);
        }

        public static bool TryGetAudit(this QueryModel model, out Audit stereotype)
        {
            if (!HasAudit(model))
            {
                stereotype = null;
                return false;
            }

            stereotype = new Audit(model.GetStereotype(Audit.DefinitionId));
            return true;
        }

        public class Audit
        {
            private IStereotype _stereotype;
            public const string DefinitionId = "481e9fcb-c990-4576-8459-f57fc0eddbf0";

            public Audit(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

        }

    }
}