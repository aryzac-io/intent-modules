using System.Collections.Generic;
using Aryzac.Api;
using Aryzac.Templates.HostedService;
using Aryzac.Templates.HostedServicesConfiguration;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace Aryzac.Templates
{
    public static class TemplateExtensions
    {
        public static string GetHostedServiceName<T>(this IIntentTemplate<T> template) where T : HostedServiceModel
        {
            return template.GetTypeName(HostedServiceTemplate.TemplateId, template.Model);
        }

        public static string GetHostedServiceName(this IIntentTemplate template, HostedServiceModel model)
        {
            return template.GetTypeName(HostedServiceTemplate.TemplateId, model);
        }

        public static string GetHostedServicesConfigurationName(this IIntentTemplate template)
        {
            return template.GetTypeName(HostedServicesConfigurationTemplate.TemplateId);
        }

    }
}