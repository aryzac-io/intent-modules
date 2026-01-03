using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.VueJS.Api;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.Modules.Common.Registrations;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TemplateRegistration.FilePerModel", Version = "1.0")]

namespace Aryzac.VueJS.Templates.Dto
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class DtoTemplateRegistration : FilePerModelTemplateRegistration<ComposableServiceModel>
    {
        private readonly IMetadataManager _metadataManager;

        public DtoTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public override string TemplateId => DtoTemplate.TemplateId;

        [IntentManaged(Mode.Fully)]
        public override ITemplate CreateTemplateInstance(IOutputTarget outputTarget, ComposableServiceModel model)
        {
            return new DtoTemplate(outputTarget, model);
        }

        [IntentManaged(Mode.Merge, Body = Mode.Ignore, Signature = Mode.Fully)]
        public override IEnumerable<ComposableServiceModel> GetModels(IApplication application)
        {
            return _metadataManager.VueJS(application).GetComposableServiceModels();
        }
    }
}