using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Domain.ValueObjects.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.GitHubIssues.Templates.Domain.DomainValueObject
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class DomainValueObjectTemplate : IntentTemplateBase<ValueObjectModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.GitHubIssues.Domain.DomainValueObject";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public DomainValueObjectTemplate(IOutputTarget outputTarget, ValueObjectModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"ValueObjects/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}