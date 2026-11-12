using System;
using System.Linq;
using Intent.Engine;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.AspNetCore.Controllers.Templates.Controller;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.Plugins;
using Intent.Plugins.FactoryExtensions;
using Intent.RoslynWeaver.Attributes;
using WorkshopModule.Templates;
using WorkshopModule.Templates.CqrsController;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.FactoryExtension", Version = "1.0")]

namespace WorkshopModule.FactoryExtensions
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public class CqrsControllerInstaller : FactoryExtensionBase
    {
        public override string Id => "WorkshopModule.CqrsControllerInstaller";

        [IntentManaged(Mode.Ignore)]
        public override int Order => 0;

        /// <summary>
        /// Dispatches the Controllers module's CQRS controllers through <c>IDispatcher</c>. A Query operation is kept
        /// only when some template fills the <c>Application.Query</c> role for that Query.
        /// </summary>
        [IntentManaged(Mode.Ignore)]
        protected override void OnAfterTemplateRegistrations(IApplication application)
        {
            // Every host project gets its own controller instance, so always loop over all of them.
            var templates = application
                .FindTemplateInstances<IControllerTemplate<IControllerModel>>(ControllerTemplate.TemplateId)
                .Where(template => template.Model is CqrsControllerModel)
                .ToList();

            // Prune before the AfterBuild below: ControllerTemplate builds its actions from Model.Operations later.
            foreach (var model in templates.Select(template => template.Model).Distinct())
            {
                var gatedOut = model.Operations
                    .Where(operation => operation.InternalElement.IsQueryModel() &&
                        !application.TemplateExists("Application.Query", operation.InternalElement.Id))
                    .ToList();
                foreach (var operation in gatedOut)
                {
                    model.Operations.Remove(operation);
                }
            }

            foreach (var template in templates)
            {
                template.CSharpFile.AfterBuild(file =>
                {
                    var @class = file.Classes.First();

                    var constructor = @class.Constructors.First();
                    if (constructor.Parameters.All(parameter => parameter.Name != "dispatcher"))
                    {
                        constructor.AddParameter(template.GetIDispatcherName(), "dispatcher", parameter =>
                            parameter.IntroduceReadonlyField());
                    }

                    foreach (var method in @class.Methods)
                    {
                        if (!method.TryGetMetadata<IControllerOperationModel>("model", out var operation))
                        {
                            continue;
                        }

                        // POST/PUT/PATCH: Controllers folds the fields into one body parameter that carries the
                        // operation's own id. GET/DELETE: each field is its own parameter, so build the request.
                        var payload = method.Parameters.FirstOrDefault(parameter =>
                            parameter.TryGetMetadata<IControllerParameterModel>("model", out var parameterModel) &&
                            parameterModel.Id == operation.Id);

                        string argument;
                        if (payload != null)
                        {
                            argument = payload.Name;
                        }
                        else
                        {
                            if (!template.TryGetTypeName(new[] { "Application.Command", "Application.Query" }, operation.InternalElement.Id, out var requestType))
                            {
                                throw new InvalidOperationException(
                                    $"No Application.Command or Application.Query template found for '{operation.InternalElement.Name}'.");
                            }

                            var arguments = method.Parameters
                                .Where(parameter => parameter.HasMetadata("model"))
                                .Select(parameter => parameter.Name);
                            argument = $"new {requestType}({string.Join(", ", arguments)})";
                        }

                        method.AddStatement($"var result = await _dispatcher.Send({argument}, cancellationToken);");
                        method.AddStatement(template.GetReturnStatement(operation));
                    }
                }, 500);
            }
        }

        /// <summary>
        /// This is an example override which would extend the
        /// <see cref="ExecutionLifeCycleSteps.BeforeTemplateExecution"/> phase of the Software Factory execution.
        /// See <see cref="FactoryExtensionBase"/> for all available overrides.
        /// </summary>
        /// <remarks>
        /// It is safe to update or delete this method.
        /// </remarks>
        protected override void OnBeforeTemplateExecution(IApplication application)
        {
        // Your custom logic here.
        }
    }
}
