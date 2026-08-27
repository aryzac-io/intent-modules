using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.Plugins;
using Intent.Plugins.FactoryExtensions;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.FactoryExtension", Version = "1.0")]

namespace Aryzac.Security.Service.FactoryExtensions
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public class SecurityAuthorityModuleDependencyValidationExtension : FactoryExtensionBase
    {
        public override string Id => "Aryzac.Security.Service.SecurityAuthorityModuleDependencyValidationExtension";

        [IntentManaged(Mode.Ignore)]
        public override int Order => 10000;

        /// <summary>
        /// This is an example override which would extend the
        /// <see cref="ExecutionLifeCycleSteps.AfterTemplateRegistrations"/> phase of the Software Factory execution.
        /// See <see cref="FactoryExtensionBase"/> for all available overrides.
        /// </summary>
        /// <remarks>
        /// It is safe to update or delete this method.
        /// </remarks>
        protected override void OnAfterTemplateRegistrations(IApplication application)
        {
            SecurityAuthorityIntegration.Initialize(application);
        }

        /// <summary>
        /// This is an example override which would extend the
        /// <see cref="ExecutionLifeCycleSteps.BeforeTemplateExecution"/> phase of the Software Factory execution.
        /// See <see cref="FactoryExtensionBase"/> for all available overrides.
        /// </summary>
        /// <remarks>
        /// It is safe to update or delete this method.
        /// </remarks>
        protected override void OnBeforeTemplateExecution(IApplication application)
        {
            SecurityAuthorityIntegration.ValidateCompanionModule(application);
            var capabilities = SecurityAuthorityIntegration.DiscoverCapabilities(application);
            SecurityAuthorityIntegration.ValidateAspNetCoreHost(capabilities);
            SecurityAuthorityIntegration.ValidatePersistenceCapability(capabilities);
        }
    }
}