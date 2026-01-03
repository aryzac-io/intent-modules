using System.Collections.Generic;
using Aryzac.Security.Templates.ScopePermissionMap;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace Aryzac.Security.Templates
{
    public static class TemplateExtensions
    {
        public static string GetScopePermissionMapName(this IIntentTemplate template)
        {
            return template.GetTypeName(ScopePermissionMapTemplate.TemplateId);
        }

    }
}