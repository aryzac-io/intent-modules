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

namespace Aryzac.VueJS.Templates.Composable
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class ComposableTemplateRegistration : FilePerModelTemplateRegistration<ComposableModel>
    {
        private readonly IMetadataManager _metadataManager;

        public ComposableTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public override string TemplateId => ComposableTemplate.TemplateId;

        [IntentManaged(Mode.Fully)]
        public override ITemplate CreateTemplateInstance(IOutputTarget outputTarget, ComposableModel model)
        {
            return new ComposableTemplate(outputTarget, model);
        }

        [IntentManaged(Mode.Merge, Body = Mode.Ignore, Signature = Mode.Fully)]
        public override IEnumerable<ComposableModel> GetModels(IApplication application)
        {
            return _metadataManager.VueJS(application).GetComposableModels();
        }
    }
}