using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.Modules.Common.FileBuilders.DataFileBuilder;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace Aryzac.Aspire.Host.Templates.AppSettings
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    public class AppSettingsTemplate : IntentTemplateBase<object>, IDataFileBuilderTemplate
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.Aspire.Host.AppSettings";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public AppSettingsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            DataFile = new DataFile($"appsettings")
                .WithJsonWriter()
                .WithRootObject(this, logging =>
                {
                    logging
                        .WithObject("Logging", logLevel =>
                        {
                            logLevel
                            .WithObject("LogLevel", logLevels =>
                            {
                                logLevels.WithValue("Default", "Information");
                                logLevels.WithValue("Microsoft.AspNetCore", "Warning");
                                logLevels.WithValue("Aspire.Hosting.Dcp", "Warning");
                            });
                        })
                    ;
                });
        }

        [IntentManaged(Mode.Fully)]
        public IDataFile DataFile { get; }

        [IntentManaged(Mode.Fully)]
        public override ITemplateFileConfig GetTemplateFileConfig() => DataFile.GetConfig();

        [IntentManaged(Mode.Fully)]
        public override string TransformText() => DataFile.ToString();
    }
}