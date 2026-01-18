using System;
using System.Collections.Generic;
using Intent.Engine;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.Modules.Common.TypeScript.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TypeScript.Templates.TypescriptTemplatePartial", Version = "1.0")]

namespace Aryzac.VueJS.Templates.Command
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    public partial class CommandTemplate : TypeScriptTemplateBase<Intent.Modelers.Services.CQRS.Api.CommandModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.VueJS.Command";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public CommandTemplate(IOutputTarget outputTarget, Intent.Modelers.Services.CQRS.Api.CommandModel model) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(TemplateId);
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TypeScriptFileConfig(
                overwriteBehaviour: OverwriteBehaviour.Always,
                fileName: $"{Model.Name.ToKebabCase().RemoveSuffix("-command")}.command",
                relativeLocation: $"{Model.InternalElement.ParentElement.Name.ToPascalCase()}",
                className: $"{Model.Name}"
            );
        }
    }
}