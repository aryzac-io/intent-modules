using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Domain.Services.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.Documentation.Templates.Domain.DomainService
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class DomainServiceTemplate : IntentTemplateBase<DomainServiceModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.Documentation.Domain.DomainService";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public DomainServiceTemplate(IOutputTarget outputTarget, DomainServiceModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"Services/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

    }
}