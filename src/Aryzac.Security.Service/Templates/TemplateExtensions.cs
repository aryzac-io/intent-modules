using System.Collections.Generic;
using Aryzac.Security.Service.Templates.SecurityAuthorityAuthorizationEndpoints;
using Aryzac.Security.Service.Templates.SecurityAuthorityAuthorizationEngine;
using Aryzac.Security.Service.Templates.SecurityAuthorityBootstrap;
using Aryzac.Security.Service.Templates.SecurityAuthorityCleanup;
using Aryzac.Security.Service.Templates.SecurityAuthorityConformanceTests;
using Aryzac.Security.Service.Templates.SecurityAuthorityContracts;
using Aryzac.Security.Service.Templates.SecurityAuthorityCryptography;
using Aryzac.Security.Service.Templates.SecurityAuthorityDeviceEndpoints;
using Aryzac.Security.Service.Templates.SecurityAuthorityDiscoveryEndpoints;
using Aryzac.Security.Service.Templates.SecurityAuthorityEnums;
using Aryzac.Security.Service.Templates.SecurityAuthorityExternalProviders;
using Aryzac.Security.Service.Templates.SecurityAuthorityIntegrationEvents;
using Aryzac.Security.Service.Templates.SecurityAuthorityLifecycle;
using Aryzac.Security.Service.Templates.SecurityAuthorityManagementEndpoints;
using Aryzac.Security.Service.Templates.SecurityAuthorityOptions;
using Aryzac.Security.Service.Templates.SecurityAuthorityPostCommitDispatch;
using Aryzac.Security.Service.Templates.SecurityAuthorityRecords;
using Aryzac.Security.Service.Templates.SecurityAuthoritySessionEndpoints;
using Aryzac.Security.Service.Templates.SecurityAuthorityTokenEndpoint;
using Aryzac.Security.Service.Templates.SecurityAuthorityValidation;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace Aryzac.Security.Service.Templates
{
    public static class TemplateExtensions
    {
        public static string GetSecurityAuthorityAuthorizationEndpointsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityAuthorizationEndpointsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityAuthorizationEngineName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityAuthorizationEngineTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityBootstrapName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityBootstrapTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityCleanupName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityCleanupTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityConformanceTestsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityConformanceTestsTemplate.TemplateId);
        }
        public static string GetSecurityAuthorityContractsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityContractsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityCryptographyName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityCryptographyTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityDeviceEndpointsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityDeviceEndpointsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityDiscoveryEndpointsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityDiscoveryEndpointsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityEnumsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityEnumsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityExternalProvidersName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityExternalProvidersTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityIntegrationEventsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityIntegrationEventsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityLifecycleName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityLifecycleTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityManagementEndpointsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityManagementEndpointsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityOptionsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityOptionsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityPostCommitDispatchName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityPostCommitDispatchTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityRecordsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityRecordsTemplate.TemplateId);
        }

        public static string GetSecurityAuthoritySessionEndpointsName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthoritySessionEndpointsTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityTokenEndpointName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityTokenEndpointTemplate.TemplateId);
        }

        public static string GetSecurityAuthorityValidationName(this IIntentTemplate template)
        {
            return template.GetTypeName(SecurityAuthorityValidationTemplate.TemplateId);
        }

    }
}