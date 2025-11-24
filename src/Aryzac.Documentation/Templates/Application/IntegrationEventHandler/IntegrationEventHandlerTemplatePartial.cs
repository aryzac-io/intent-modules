using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Services.EventInteractions;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.Documentation.Templates.Application.IntegrationEventHandler
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class IntegrationEventHandlerTemplate : IntentTemplateBase<IntegrationEventHandlerModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.Documentation.Application.IntegrationEventHandler";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public IntegrationEventHandlerTemplate(IOutputTarget outputTarget, IntegrationEventHandlerModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"IntegrationEvents/Handlers/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}