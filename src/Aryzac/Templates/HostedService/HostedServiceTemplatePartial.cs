using System;
using System.Collections.Generic;
using Aryzac.Api;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Templates.HostedService
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class HostedServiceTemplate : CSharpTemplateBase<HostedServiceModel>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.HostedService";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public HostedServiceTemplate(IOutputTarget outputTarget, HostedServiceModel model) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("MediatR")
                .AddClass($"{Model.Name}HostedService", @class =>
                {
                    @class.ImplementsInterface("IHostedService");

                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("IHostApplicationLifetime", "appLifetime", param =>
                        {
                            param.IntroduceReadonlyField();
                        });
                        ctor.AddParameter("IServiceProvider", "serviceProvider", param =>
                        {
                            param.IntroduceReadonlyField();
                        });
                    });

                    @class.AddMethod("Task", "StartAsync", method =>
                    {
                        method.AddParameter("CancellationToken", "cancellationToken");

                        method.AddStatement("_appLifetime.ApplicationStarted.Register(async () => await OnStarted(cancellationToken));");
                        method.AddStatement("_appLifetime.ApplicationStopping.Register(async () => await OnStopping(cancellationToken));");

                        method.AddStatement("return Task.CompletedTask;");
                    });

                    @class.AddMethod("Task", "StopAsync", method =>
                    {
                        method.AddParameter("CancellationToken", "cancellationToken");

                        method.AddStatement("return Task.CompletedTask;");
                    });

                    @class.AddMethod("Task", "OnStarted", method =>
                    {
                        method.Async();
                        method.Private();
                        method.AddParameter("CancellationToken", "cancellationToken");

                        method.AddStatement("using (var scope = _serviceProvider.CreateScope())");
                        method.AddStatement("{");
                        method.AddStatement("var mediator = scope.ServiceProvider.GetRequiredService<ISender>();");
                        method.AddStatement("}");
                    });

                    @class.AddMethod("Task", "OnStopping", method =>
                    {
                        method.Async();
                        method.Private();
                        method.AddParameter("CancellationToken", "cancellationToken");

                        method.AddStatement("using (var scope = _serviceProvider.CreateScope())");
                        method.AddStatement("{");
                        method.AddStatement("var mediator = scope.ServiceProvider.GetRequiredService<ISender>();");
                        method.AddStatement("}");
                    });
                });
        }

        [IntentManaged(Mode.Fully)]
        public CSharpFile CSharpFile { get; }

        [IntentManaged(Mode.Fully)]
        protected override CSharpFileConfig DefineFileConfig()
        {
            return CSharpFile.GetConfig();
        }

        [IntentManaged(Mode.Fully)]
        public override string TransformText()
        {
            return CSharpFile.ToString();
        }
    }
}