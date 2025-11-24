using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.GitHubIssues.Templates.Domain.DomainDto
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class DomainDtoTemplate : IntentTemplateBase<DTOModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.GitHubIssues.Domain.DomainDto";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public DomainDtoTemplate(IOutputTarget outputTarget, DTOModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"Dtos/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}