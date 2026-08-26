using System.Collections.Generic;
using Aryzac.Security.Templates.ScopePermissionMap;
using Aryzac.Security.Templates.SecurityContractFoundation;
using Aryzac.Security.Templates.SecurityInboundCredentials;
using Aryzac.Security.Templates.SecurityRegistration;
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

        public static string GetSecurityContractFoundationName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityContractFoundationTemplate.TemplateId);
        }

        public static string GetSecurityInboundCredentialsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityInboundCredentialsTemplate.TemplateId);
        }

        public static string GetSecurityRegistrationName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityRegistrationTemplate.TemplateId);
        }

    }
}