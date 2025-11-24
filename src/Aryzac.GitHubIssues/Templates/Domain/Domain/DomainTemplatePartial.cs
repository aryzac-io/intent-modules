using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.GitHubIssues.Api;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Domain.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.GitHubIssues.Templates.Domain.Domain
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class DomainTemplate : IntentTemplateBase<ClassModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.GitHubIssues.Domain.Domain";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public DomainTemplate(IOutputTarget outputTarget, ClassModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            Model.HasBusinessRequirement();
            return new TemplateFileConfig(
                fileName: $"Entities/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}