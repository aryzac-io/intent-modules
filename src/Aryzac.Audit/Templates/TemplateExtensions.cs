using System.Collections.Generic;
using Aryzac.Audit.Templates.AuditInterface;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace Aryzac.Audit.Templates
{
    public static class TemplateExtensions
    {
        public static string GetAuditInterfaceName(this IIntentTemplate template)
        {
            return template.GetTypeName(AuditInterfaceTemplate.TemplateId);
        }

    }
}