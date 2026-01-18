using System.Collections.Generic;
using Aryzac.VueJS.Api;
using Aryzac.VueJS.Templates.Command;
using Aryzac.VueJS.Templates.Composable;
using Aryzac.VueJS.Templates.Dto;
using Aryzac.VueJS.Templates.EnumType;
using Aryzac.VueJS.Templates.JsonReponse;
using Aryzac.VueJS.Templates.Query;
using Intent.Modelers.Services.Api;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common.Templates;
using Intent.Modules.Common.Types.Api;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace Aryzac.VueJS.Templates
{
    public static class TemplateExtensions
    {
        public static string GetCommandTemplateName<T>(this IIntentTemplate<T> template) where T : CommandModel
        {
            return template.GetTypeName(CommandTemplate.TemplateId, template.Model);
        }

        public static string GetCommandTemplateName(this IIntentTemplate template, CommandModel model)
        {
            return template.GetTypeName(CommandTemplate.TemplateId, model);
        }
        public static string GetComposableTemplateName<T>(this IIntentTemplate<T> template) where T : ComposableModel
        {
            return template.GetTypeName(ComposableTemplate.TemplateId, template.Model);
        }

        public static string GetComposableTemplateName(this IIntentTemplate template, ComposableModel model)
        {
            return template.GetTypeName(ComposableTemplate.TemplateId, model);
        }

        public static string GetDtoTemplateName<T>(this IIntentTemplate<T> template) where T : DTOModel
        {
            return template.GetTypeName(DtoTemplate.TemplateId, template.Model);
        }

        public static string GetDtoTemplateName(this IIntentTemplate template, DTOModel model)
        {
            return template.GetTypeName(DtoTemplate.TemplateId, model);
        }

        public static string GetEnumTypeTemplateName<T>(this IIntentTemplate<T> template) where T : EnumModel
        {
            return template.GetTypeName(EnumTypeTemplate.TemplateId, template.Model);
        }

        public static string GetEnumTypeTemplateName(this IIntentTemplate template, EnumModel model)
        {
            return template.GetTypeName(EnumTypeTemplate.TemplateId, model);
        }

        public static string GetJsonReponseTemplateName(this IIntentTemplate template)
        {
            return template.GetTypeName(JsonReponseTemplate.TemplateId);
        }

        public static string GetQueryTemplateName<T>(this IIntentTemplate<T> template) where T : QueryModel
        {
            return template.GetTypeName(QueryTemplate.TemplateId, template.Model);
        }

        public static string GetQueryTemplateName(this IIntentTemplate template, QueryModel model)
        {
            return template.GetTypeName(QueryTemplate.TemplateId, model);
        }

    }
}