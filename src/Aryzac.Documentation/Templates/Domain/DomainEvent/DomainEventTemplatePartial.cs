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

namespace Aryzac.Documentation.Templates.Domain.DomainEvent
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class DomainEventTemplate : IntentTemplateBase<DomainEventModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.Documentation.Domain.DomainEvent";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public DomainEventTemplate(IOutputTarget outputTarget, DomainEventModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"Events/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}