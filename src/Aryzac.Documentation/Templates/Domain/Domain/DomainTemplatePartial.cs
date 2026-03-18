using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Domain.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.Modules.Common.Types.Api;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.Documentation.Templates.Domain.Domain
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class DomainTemplate : IntentTemplateBase<ClassModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.Documentation.Domain.Domain";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public DomainTemplate(IOutputTarget outputTarget, ClassModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            var applicationParts = Model.Application.Name.ToKebabCase().Replace(".", "-").Replace("-", "/").Split("/");
            var application = string.Join("/", applicationParts.Where(x => !string.Equals(x, "verentis", StringComparison.OrdinalIgnoreCase)));
            var folders = this.GetFolderPath().ToLower();

            return new TemplateFileConfig(
                overwriteBehaviour: OverwriteBehaviour.Always,
                fileName: $"{Model.Name.ToKebabCase()}",
                fileExtension: "md",
                relativeLocation: $"../../../../docs/content/{application}/{folders}"
            );
        }

    }
}