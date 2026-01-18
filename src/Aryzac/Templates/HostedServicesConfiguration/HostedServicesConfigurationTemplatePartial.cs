using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.Api;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.DependencyInjection;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Templates.HostedServicesConfiguration
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class HostedServicesConfigurationTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.HostedServicesConfiguration";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public HostedServicesConfigurationTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("Microsoft.Extensions.DependencyInjection")
                .AddClass($"HostedServicesConfiguration", @class =>
                {
                    @class.Static();
                    @class.AddMethod("IServiceCollection", "ConfigureHostedServices", method =>
                    {
                        method.Static();
                        method.AddParameter("IServiceCollection", "services", param => param.WithThisModifier());

                        var hostedServices = GetHostedServices(outputTarget);

                        foreach (var hostedService in hostedServices)
                        {
                            method.AddStatement($"services.AddHostedService<{hostedService.Name}HostedService>();");
                        }

                        method.AddStatement("return services;");
                    });
                });
        }

        private const string hostedServiceTypeId = "a7448be4-7ee9-45b9-b399-4beb4fad98d8";

        private IEnumerable<HostedServiceModel> GetHostedServices(IOutputTarget outputTarget)
        {
            var servicesDesigner = outputTarget.ExecutionContext.MetadataManager.GetDesigner(outputTarget.ExecutionContext.GetApplicationConfig().Id, "Services");

            return servicesDesigner.GetElementsOfType(hostedServiceTypeId).Select(m => m.AsHostedServiceModel());
        }

        public override void BeforeTemplateExecution()
        {
            if (!CanRunTemplate()) return;
            ExecutionContext.EventDispatcher.Publish(ServiceConfigurationRequest
                .ToRegister("ConfigureHostedServices")
                .HasDependency(this));
        }

        [IntentManaged(Mode.Fully)]
        public CSharpFile CSharpFile { get; }

        [IntentManaged(Mode.Fully)]
        protected override CSharpFileConfig DefineFileConfig()
        {
            return CSharpFile.GetConfig();
        }

        [IntentManaged(Mode.Fully)]
        public override string TransformText()
        {
            return CSharpFile.ToString();
        }
    }
}