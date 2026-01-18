using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.VueJS.Api;
using Aryzac.VueJS.Templates.Dto;
using Intent.Engine;
using Intent.Metadata.ApiGateway.Api;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Registrations;
using Intent.Modules.Common.Types.Api;
using Intent.Registrations;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TemplateRegistration.Custom", Version = "1.0")]

namespace Aryzac.VueJS.Templates.EnumType
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class EnumTypeTemplateRegistration : ITemplateRegistration
    {
        private readonly IMetadataManager _metadataManager;

        public EnumTypeTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public string TemplateId => EnumTypeTemplate.TemplateId;

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public void DoRegistration(ITemplateInstanceRegistry registry, IApplication applicationManager)
        {
            var services = _metadataManager.VueJS(applicationManager).GetComposableServiceModels();

            var enums = new List<EnumModel>();
            var visitedDtos = new HashSet<string>(StringComparer.Ordinal);
            var visitedEnums = new HashSet<string>(StringComparer.Ordinal);

            foreach (var service in services)
            {
                var requestType = GetRouteRequestType(service);

                var requestCommandType = requestType.AsCommandModel();
                if (requestCommandType != null)
                {
                    foreach (var property in requestCommandType.Properties)
                    {
                        AddPropertyEnums(property, enums, visitedDtos, visitedEnums);
                    }
                }

                var requestQueryType = requestType.AsQueryModel();
                if (requestQueryType != null)
                {
                    foreach (var property in requestQueryType.Properties)
                    {
                        AddPropertyEnums(property, enums, visitedDtos, visitedEnums);
                    }
                }

                var responseType = GetRouteResponseType(service);
                if (responseType.AsDTOModel() is DTOModel dtoModel)
                {
                    AddDtoRecursive(dtoModel, enums, visitedDtos, visitedEnums);
                }
            }

            foreach (var @enum in enums)
            {
                registry.RegisterTemplate(TemplateId, project => new EnumTypeTemplate(project, @enum));
            }
        }

        private static void AddPropertyEnums(
            DTOFieldModel property,
            List<EnumModel> enums,
            HashSet<string> visitedDtos,
            HashSet<string> visitedEnums)
        {
            if (property.TypeReference.Element.AsDTOModel() is DTOModel dtoModel)
            {
                AddDtoRecursive(dtoModel, enums, visitedDtos, visitedEnums);
            }

            if (property.TypeReference.Element.AsEnumModel() is EnumModel enumModel)
            {
                AddEnum(enumModel, enums, visitedEnums);
            }
        }

        private static void AddDtoRecursive(
            DTOModel dtoModel,
            List<EnumModel> enums,
            HashSet<string> visitedDtos,
            HashSet<string> visitedEnums)
        {
            // Prefer a stable unique key. Id is typically the best choice in Intent models.
            if (!visitedDtos.Add(dtoModel.Id))
            {
                return; // already processed
            }

            foreach (var dtoField in dtoModel.Fields)
            {
                // If you need to support DTOs hidden behind wrappers (e.g. collections),
                // consider also checking dtoField.TypeReference.Element.AsDTOModel().
                if (dtoField.TypeReference.Element.AsDTOModel() is DTOModel nestedDtoModel)
                {
                    AddDtoRecursive(nestedDtoModel, enums, visitedDtos, visitedEnums);
                }

                if (dtoField.TypeReference.Element.AsEnumModel() is EnumModel enumModel)
                {
                    AddEnum(enumModel, enums, visitedEnums);
                }
            }
        }

        private static void AddEnum(EnumModel enumModel, List<EnumModel> enums, HashSet<string> visitedEnums)
        {
            if (visitedEnums.Add(enumModel.Id))
            {
                enums.Add(enumModel);
            }
        }

        public IElement GetRouteResponseType(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            return (IElement)endpoint.TypeReference.Element;
        }

        public IElement GetRouteRequestType(ComposableServiceModel service)
        {
            var serviceSettings = service.GetServiceSettings();
            var serviceProperty = serviceSettings.Service();

            var apiGatewayRoute = serviceProperty.AsApiGatewayRouteModel();
            var endpoint = apiGatewayRoute.DownstreamEndpoints()[0].Element;

            return (IElement)endpoint;
        }
    }
}
