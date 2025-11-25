using System.Collections.Generic;
using Aryzac.Aspire.Host.Templates.AppHost;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace Aryzac.Aspire.Host.Templates
{
    public static class TemplateExtensions
    {
        public static string GetAppHostName(this IIntentTemplate template)
        {
            return template.GetTypeName(AppHostTemplate.TemplateId);
        }

    }
}