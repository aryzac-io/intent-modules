using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Domain.Events.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.Documentation.Templates.Application.DomainEventHandler
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class DomainEventHandlerTemplate : IntentTemplateBase<DomainEventHandlerModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.Documentation.Application.DomainEventHandler";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public DomainEventHandlerTemplate(IOutputTarget outputTarget, DomainEventHandlerModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"Events/Handlers/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}