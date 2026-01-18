using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Aryzac.VueJS.Api;
using Intent.Engine;
using Intent.Metadata.ApiGateway.Api;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Registrations;
using Intent.Registrations;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;
using static System.Net.Mime.MediaTypeNames;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TemplateRegistration.Custom", Version = "1.0")]

namespace Aryzac.VueJS.Templates.Dto
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class DtoTemplateRegistration : ITemplateRegistration
    {
        private readonly IMetadataManager _metadataManager;

        public DtoTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public string TemplateId => DtoTemplate.TemplateId;

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public void DoRegistration(ITemplateInstanceRegistry registry, IApplication applicationManager)
        {
            var services = _metadataManager.VueJS(applicationManager).GetComposableServiceModels();

            var dtos = new List<DTOModel>();
            var visited = new HashSet<string>(StringComparer.Ordinal);

            foreach (var service in services)
            {
                var requestType = GetRouteRequestType(service);

                var requestCommandType = requestType.AsCommandModel();
                if (requestCommandType != null)
                {
                    foreach (var property in requestCommandType.Properties)
                    {
                        if (property.TypeReference.Element.AsDTOModel() is DTOModel commandDtoModel)
                        {
                            AddDtoRecursive(commandDtoModel, dtos, visited);
                        }
                    }
                }

                var requestQueryType = requestType.AsQueryModel();
                if (requestQueryType != null)
                {
                    foreach (var property in requestQueryType.Properties)
                    {
                        if (property.TypeReference.Element.AsDTOModel() is DTOModel queryDtoModel)
                        {
                            AddDtoRecursive(queryDtoModel, dtos, visited);
                        }
                    }
                }

                var responseType = GetRouteResponseType(service);

                if (responseType.AsDTOModel() is DTOModel dtoModel)
                {
                    AddDtoRecursive(dtoModel, dtos, visited);
                }
            }

            foreach (var dto in dtos)
            {
                registry.RegisterTemplate(TemplateId, project => new DtoTemplate(project, dto));
            }
        }

        private static void AddDtoRecursive(DTOModel dtoModel, List<DTOModel> dtos, HashSet<string> visited)
        {
            // Prefer a stable unique key. Id is typically the best choice in Intent models.
            if (!visited.Add(dtoModel.Id))
            {
                return; // already processed
            }

            dtos.Add(dtoModel);

            foreach (var dtoField in dtoModel.Fields)
            {
                // If you need to support DTOs hidden behind wrappers (e.g. collections),
                // consider also checking dtoField.TypeReference.Element.AsDTOModel().
                if (dtoField.TypeReference.Element.AsDTOModel() is DTOModel nestedDtoModel)
                {
                    AddDtoRecursive(nestedDtoModel, dtos, visited);
                }
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
