using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.GitHubIssues.Templates.Api.HttpEndpointCommand
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    partial class HttpEndpointCommandTemplate : IntentTemplateBase<CommandModel>
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.GitHubIssues.Api.HttpEndpointCommand";

        [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
        public HttpEndpointCommandTemplate(IOutputTarget outputTarget, CommandModel model) : base(TemplateId, outputTarget, model)
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TemplateFileConfig(
                fileName: $"Commands/{this.GetFolderPath()}/{Model.Name}",
                fileExtension: "md"
            );
        }

        public override bool CanRunTemplate()
        {
            return Model.GetStereotypes("Http Settings").Any();
        }

        public Intent.Metadata.Models.IStereotype GetHttpSettings()
        {
            return Model.GetStereotypes("Http Settings").First();
        }

        public IEnumerable<Intent.Metadata.Models.IStereotypeProperty> GetHttpSettingsProperties()
        {
            return GetHttpSettings().Properties;
        }

        public string GetHttpSettingsDefinitionId()
        {
            return GetHttpSettings().DefinitionId;
        }

        public string GetVerb()
        {
            return GetHttpSettingsProperties().First(p => p.Key == "Verb").Value;
        }

        public string GetRoute()
        {
            return GetHttpSettingsProperties().First(p => p.Key == "Route").Value;
        }
    }
}