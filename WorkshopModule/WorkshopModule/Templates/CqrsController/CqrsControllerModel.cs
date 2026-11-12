using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modules.AspNetCore.Controllers.Templates.Controller;
using Intent.Modules.AspNetCore.Controllers.Templates.Controller.Models;
using Intent.Modules.Common.Types.Api;
using Intent.Modules.Metadata.Security.Models;
using Intent.Modules.Metadata.WebApi.Models;

namespace WorkshopModule.Templates.CqrsController
{
    /// <summary>
    /// One controller per Services designer folder, holding the HTTP endpoints of the Commands and Queries in it.
    /// </summary>
    public class CqrsControllerModel : IControllerModel
    {
        public CqrsControllerModel(IElement folderElement, IEnumerable<IHttpEndpointModel> endpoints)
        {
            Id = folderElement.Id;
            Name = folderElement.Name;
            // The parent of the grouping folder, as Controllers' own CQRS dispatch modules do: the controller
            // lands in Controllers/, not Controllers/<Folder>/.
            Folder = folderElement.ParentElement?.AsFolderModel();
            InternalElement = folderElement;
            Operations = endpoints
                .Select(endpoint => (IControllerOperationModel)new ControllerOperationModel(endpoint, new List<IApiVersionModel>(), this))
                .ToList();
        }

        public string Id { get; }
        public string Name { get; }
        public FolderModel Folder { get; }
        public string Route => null;
        public string Comment => null;
        public bool RequiresAuthorization => false;
        public bool AllowAnonymous => false;
        public IReadOnlyCollection<ISecurityModel> SecurityModels { get; } = new ISecurityModel[0];
        public IList<IControllerOperationModel> Operations { get; }
        public IList<IApiVersionModel> ApplicableVersions { get; } = new List<IApiVersionModel>();
        public IElement InternalElement { get; }
    }
}
