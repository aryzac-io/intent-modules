using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modelers.Domain.Api;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModelExtensions", Version = "1.0")]

namespace Aryzac.Audit.Api
{
    public static class ClassModelStereotypeExtensions
    {
        public static AuditTable GetAuditTable(this ClassModel model)
        {
            var stereotype = model.GetStereotype(AuditTable.DefinitionId);
            return stereotype != null ? new AuditTable(stereotype) : null;
        }


        public static bool HasAuditTable(this ClassModel model)
        {
            return model.HasStereotype(AuditTable.DefinitionId);
        }

        public static bool TryGetAuditTable(this ClassModel model, out AuditTable stereotype)
        {
            if (!HasAuditTable(model))
            {
                stereotype = null;
                return false;
            }

            stereotype = new AuditTable(model.GetStereotype(AuditTable.DefinitionId));
            return true;
        }

        public class AuditTable
        {
            private IStereotype _stereotype;
            public const string DefinitionId = "c90080ae-bf89-4d10-b398-759e07630f98";

            public AuditTable(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

        }

    }
}