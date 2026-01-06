using System.Linq;
using Aryzac.Audit.Templates.AuditInterface;
using Intent.Engine;
using Intent.Modelers.Domain.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Plugins;
using Intent.Modules.Common.Templates;
using Intent.Plugins.FactoryExtensions;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.FactoryExtension", Version = "1.0")]

namespace Aryzac.Audit.FactoryExtensions
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public class AuditExtension : FactoryExtensionBase
    {
        public override string Id => "Aryzac.Audit.AuditExtension";

        [IntentManaged(Mode.Ignore)]
        public override int Order => 0;

        /// <summary>
        /// This is an example override which would extend the
        /// <see cref="ExecutionLifeCycleSteps.AfterTemplateRegistrations"/> phase of the Software Factory execution.
        /// See <see cref="FactoryExtensionBase"/> for all available overrides.
        /// </summary>
        /// <remarks>
        /// It is safe to update or delete this method.
        /// </remarks>
        protected override void OnAfterTemplateRegistrations(IApplication application)
        {
            ApplyAuditInterfaceToTemplate(application, "Application.Query");
            ApplyAuditInterfaceToTemplate(application, "Application.Command");
        }

        private static void ApplyAuditInterfaceToTemplate(IApplication application, string t)
        {
            var handlerTemplates = application.FindTemplateInstances<ICSharpFileBuilderTemplate>(TemplateDependency.OnTemplate(t));
            foreach (var template in handlerTemplates)
            {
                if (!template.TryGetModel<ClassModel>(out var templateModel) ||
                    !templateModel.HasStereotype("481e9fcb-c990-4576-8459-f57fc0eddbf0"))
                {
                    continue;
                }
                template.CSharpFile.OnBuild(file =>
                {
                    var @class = file.Classes.FirstOrDefault();

                    if (@class.TryGetMetadata<ClassModel>("model", out var model))
                    {
                        @class.ImplementsInterface(template.GetTypeName(AuditInterfaceTemplate.TemplateId));
                    }
                });
            }
        }
    }
}