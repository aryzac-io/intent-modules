using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Eventing.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.GitHubIssues.Templates.Application.IntegrationEvent
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class IntegrationEventTemplate : IntentTemplateBase<MessageModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.GitHubIssues.Application.IntegrationEvent";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public IntegrationEventTemplate(IOutputTarget outputTarget, MessageModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"IntegrationEvents/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}