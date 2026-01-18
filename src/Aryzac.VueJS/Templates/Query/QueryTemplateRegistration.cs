using System;
using System.Collections.Generic;
using System.Linq;
using Aryzac.VueJS.Api;
using Aryzac.VueJS.Templates.Command;
using Intent.Engine;
using Intent.Metadata.ApiGateway.Api;
using Intent.Metadata.Models;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Registrations;
using Intent.Registrations;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TemplateRegistration.Custom", Version = "1.0")]

namespace Aryzac.VueJS.Templates.Query
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class QueryTemplateRegistration : ITemplateRegistration
    {
        private readonly IMetadataManager _metadataManager;

        public QueryTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public string TemplateId => QueryTemplate.TemplateId;

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public void DoRegistration(ITemplateInstanceRegistry registry, IApplication applicationManager)
        {
            var services = _metadataManager.VueJS(applicationManager).GetComposableServiceModels();

            var queries = new List<QueryModel>();

            foreach (var service in services)
            {
                var requestType = GetRouteRequestType(service);

                if (requestType.AsQueryModel() is QueryModel queryModel)
                {
                    registry.RegisterTemplate(TemplateId, project => new QueryTemplate(project, queryModel));
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