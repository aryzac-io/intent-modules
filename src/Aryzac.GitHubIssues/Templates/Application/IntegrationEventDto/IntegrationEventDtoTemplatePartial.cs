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

namespace Aryzac.GitHubIssues.Templates.Application.IntegrationEventDto
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class IntegrationEventDtoTemplate : IntentTemplateBase<EventingDTOModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.GitHubIssues.Application.IntegrationEventDto";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public IntegrationEventDtoTemplate(IOutputTarget outputTarget, EventingDTOModel model) : base(TemplateId, outputTarget, model)
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