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

namespace Aryzac.VueJS.Templates.Query
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    public partial class QueryTemplate : TypeScriptTemplateBase<Intent.Modelers.Services.CQRS.Api.QueryModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.VueJS.Query";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public QueryTemplate(IOutputTarget outputTarget, Intent.Modelers.Services.CQRS.Api.QueryModel model) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(TemplateId);
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TypeScriptFileConfig(
                overwriteBehaviour: OverwriteBehaviour.Always,
                fileName: $"{Model.Name.ToKebabCase().RemoveSuffix("-query")}.query",
                relativeLocation: $"{Model.InternalElement.ParentElement.Name.ToPascalCase()}",
                className: $"{Model.Name}"
            );
        }
    }
}