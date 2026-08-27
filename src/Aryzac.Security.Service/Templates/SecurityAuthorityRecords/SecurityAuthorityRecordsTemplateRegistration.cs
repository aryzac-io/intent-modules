using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.Modules.Common.Registrations;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TemplateRegistration.SingleFileNoModel", Version = "1.0")]

namespace Aryzac.Security.Service.Templates.SecurityAuthorityRecords
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class SecurityAuthorityRecordsTemplateRegistration : SingleFileTemplateRegistration
    {
        public override string TemplateId => SecurityAuthorityRecordsTemplate.TemplateId;

        [IntentManaged(Mode.Fully)]
        public override ITemplate CreateTemplateInstance(IOutputTarget outputTarget)
        {
            return new SecurityAuthorityRecordsTemplate(outputTarget);
        }
    }
}