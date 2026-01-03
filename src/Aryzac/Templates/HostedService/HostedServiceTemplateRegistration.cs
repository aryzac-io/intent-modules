using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.Api;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Registrations;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TemplateRegistration.FilePerModel", Version = "1.0")]

namespace Aryzac.Templates.HostedService
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class HostedServiceTemplateRegistration : FilePerModelTemplateRegistration<HostedServiceModel>
    {
        private readonly IMetadataManager _metadataManager;

        public HostedServiceTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public override string TemplateId => HostedServiceTemplate.TemplateId;

        [IntentManaged(Mode.Fully)]
        public override ITemplate CreateTemplateInstance(IOutputTarget outputTarget, HostedServiceModel model)
        {
            return new HostedServiceTemplate(outputTarget, model);
        }

        [IntentManaged(Mode.Merge, Body = Mode.Ignore, Signature = Mode.Fully)]
        public override IEnumerable<HostedServiceModel> GetModels(IApplication application)
        {
            return _metadataManager.Services(application).GetHostedServiceModels();
        }
    }
}