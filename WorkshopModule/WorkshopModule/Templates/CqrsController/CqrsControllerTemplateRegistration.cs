using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.AspNetCore.Controllers.Templates.Controller;
using Intent.Modules.Common.Registrations;
using Intent.Modules.Common.Types.Api;
using Intent.Modules.Metadata.WebApi.Models;
using Intent.Templates;

namespace WorkshopModule.Templates.CqrsController
{
    /// <summary>
    /// Contributes one <see cref="CqrsControllerModel"/> per Services folder to the Controllers module's
    /// <see cref="ControllerTemplate"/>. Queries are not filtered here: other modules' Application.Query templates
    /// may not be registered yet, so <c>CqrsControllerInstaller</c> prunes them once every template is registered.
    /// </summary>
    public class CqrsControllerTemplateRegistration : FilePerModelTemplateRegistration<IControllerModel>
    {
        private readonly IMetadataManager _metadataManager;

        public CqrsControllerTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public override string TemplateId => ControllerTemplate.TemplateId;

        public override ITemplate CreateTemplateInstance(IOutputTarget outputTarget, IControllerModel model)
        {
            return new ControllerTemplate(outputTarget, model);
        }

        public override IEnumerable<IControllerModel> GetModels(IApplication application)
        {
            var services = _metadataManager.Services(application);
            var elements = services.GetCommandModels().Select(command => command.InternalElement)
                .Concat(services.GetQueryModels().Select(query => query.InternalElement));

            var endpoints = new List<(IElement Element, IHttpEndpointModel Endpoint)>();
            foreach (var element in elements)
            {
                if (element.ParentElement?.SpecializationTypeId != FolderModel.SpecializationTypeId)
                {
                    continue;
                }

                if (application.TryGetHttpEndpoint(element, null, out var endpoint))
                {
                    endpoints.Add((element, endpoint));
                }
            }

            return endpoints
                .GroupBy(item => item.Element.ParentElement.Id)
                .Select(group => new CqrsControllerModel(
                    group.First().Element.ParentElement,
                    group.OrderBy(item => item.Element.Order).Select(item => item.Endpoint)))
                .ToList();
        }
    }
}
