using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.VueJS.Api;
using Intent.Engine;
using Intent.Metadata.ApiGateway.Api;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Templates;
using Intent.Modules.Common.TypeScript.Builder;
using Intent.Modules.Common.TypeScript.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TypeScript.Templates.TypescriptTemplatePartial", Version = "1.0")]

namespace Aryzac.VueJS.Templates.Dto
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    public partial class DtoTemplate : TypeScriptTemplateBase<Aryzac.VueJS.Api.ComposableServiceModel>, ITypescriptFileBuilderTemplate
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "Aryzac.VueJS.Dto";

        [IntentManaged(Mode.Merge, Body = Mode.Ignore, Signature = Mode.Fully)]
        public DtoTemplate(IOutputTarget outputTarget, Aryzac.VueJS.Api.ComposableServiceModel model) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(TemplateId);

            TypescriptFile = new TypescriptFile(this.GetFolderPath());

            var requestModel = GetRouteRequestType(model);
            CreateTypeScriptInterface(requestModel);

            var responseModel = GetRouteResponseType(model);
            CreateTypeScriptInterface(responseModel);
        }

        private void CreateTypeScriptInterface(DTOModel model)
        {
            if (model == null) {
                return;
            }

            TypescriptFile.AddInterface($"{model.Name}", iface =>
            {
                iface.Export();

                foreach (var field in model.Fields)
                {
                    var tsType = GetTypeName(field.TypeReference);

                    if (field.TypeReference.Element.IsDTOModel())
                    {
                        CreateTypeScriptInterface(field.TypeReference.Element.AsDTOModel());
                    }

                    iface.AddField(field.Name.ToCamelCase(), tsType);
                }
            });
        }

        public DTOModel GetRouteResponseType(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            return endpoint.TypeReference.Element.AsDTOModel();
        }

        public DTOModel GetRouteRequestType(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            return endpoint.AsDTOModel();
        }

        [IntentManaged(Mode.Fully)]
        public TypescriptFile TypescriptFile { get; }

        [IntentManaged(Mode.Merge, Body = Mode.Ignore)]
        public override ITemplateFileConfig GetTemplateFileConfig()
        {
            return new TypeScriptFileConfig(
                overwriteBehaviour: OverwriteBehaviour.Always,
                fileName: $"{Model.Name.ToKebabCase()}",
                relativeLocation: $"{Model.InternalElement.ParentElement.Name.ToKebabCase()}",
                className: $"{Model.Name}"
            );
        }

        [IntentManaged(Mode.Fully)]
        public override string TransformText()
        {
            return TypescriptFile.ToString();
        }
    }
}