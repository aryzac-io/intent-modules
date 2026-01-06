using System.Collections.Generic;
using Aryzac.VueJS.Api;
using Aryzac.VueJS.Templates.Composable;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace Aryzac.VueJS.Templates
{
    public static class TemplateExtensions
    {
        public static string GetComposableTemplateName<T>(this IIntentTemplate<T> template) where T : ComposableModel
        {
            return template.GetTypeName(ComposableTemplate.TemplateId, template.Model);
        }

        public static string GetComposableTemplateName(this IIntentTemplate template, ComposableModel model)
        {
            return template.GetTypeName(ComposableTemplate.TemplateId, model);
        }

    }
}