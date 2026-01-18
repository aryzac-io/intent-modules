using System;
using System.Collections.Generic;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.Modules.Common.Types.Api;
using Intent.Modules.Common.TypeScript.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TypeScript.Templates.TypescriptTemplatePartial", Version = "1.0")]

namespace Aryzac.VueJS.Templates.EnumType
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    public partial class EnumTypeTemplate : TypeScriptTemplateBase<Intent.Modules.Common.Types.Api.EnumModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.VueJS.EnumType";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public EnumTypeTemplate(IOutputTarget outputTarget, Intent.Modules.Common.Types.Api.EnumModel model) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(TemplateId);
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TypeScriptFileConfig(
                overwriteBehaviour: OverwriteBehaviour.Always,
                fileName: $"{Model.Name.ToKebabCase()}.enum",
                relativeLocation: $"{Model.InternalElement.ParentElement.Name.ToPascalCase()}",
                className: $"{Model.Name}"
            );
        }
    }
}