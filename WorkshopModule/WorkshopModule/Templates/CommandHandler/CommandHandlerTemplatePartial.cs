using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.DependencyInjection;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;
using WorkshopModule.Templates.IAppRequestHandler;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.CommandHandler
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class CommandHandlerTemplate : CSharpTemplateBase<CommandModel>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.CommandHandler";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public CommandHandlerTemplate(IOutputTarget outputTarget, CommandModel model) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddClass($"{Model.Name}Handler")
                .OnBuild(file =>
                {
                    var response = Model.TypeReference.Element != null
                        ? GetTypeName(Model.TypeReference)
                        : "Unit";

                    var @class = file.Classes.First();
                    @class.ImplementsInterface($"{this.GetIAppRequestHandlerName()}<{this.GetCommandRequestName(Model)}, {response}>");
                    @class.AddConstructor();
                    @class.AddMethod($"{UseType("System.Threading.Tasks.Task")}<{response}>", "Handle", method =>
                    {
                        method.Async();
                        method.AddAttribute(CSharpIntentManagedAttribute.IgnoreBody());
                        method.AddParameter(this.GetCommandRequestName(Model), "request");
                        method.AddParameter(UseType("System.Threading.CancellationToken"), "cancellationToken");
                        method.AddStatement($"// TODO: Implement {method.Name} ({@class.Name}) functionality");
                        method.AddStatement("""throw new NotImplementedException("Your implementation here...");""");
                    });
                }, 0);
        }

        [IntentManaged(Mode.Ignore)]
        public override void BeforeTemplateExecution()
        {
            base.BeforeTemplateExecution();

            var response = Model.TypeReference.Element != null
                ? GetTypeName(Model.TypeReference)
                : "Unit";
            var handlerInterface = GetTemplate<IClassProvider>(IAppRequestHandlerTemplate.TemplateId);

            ExecutionContext.EventDispatcher.Publish(ContainerRegistrationRequest
                .ToRegister(this)
                .ForInterface($"{this.GetIAppRequestHandlerName()}<{this.GetCommandRequestName(Model)}, {response}>")
                .ForConcern("Application")
                .WithPerServiceCallLifeTime()
                .RequiresUsingNamespaces(handlerInterface.Namespace)
                .HasDependency(GetTemplate<ITemplate>(IAppRequestHandlerTemplate.TemplateId))
                .WithPriority(20));
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
