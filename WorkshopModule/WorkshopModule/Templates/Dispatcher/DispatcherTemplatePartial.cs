using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.DependencyInjection;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;
using WorkshopModule.Templates.IDispatcher;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.Dispatcher
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class DispatcherTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.Dispatcher";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public DispatcherTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddClass("Dispatcher")
                .OnBuild(file =>
                {
                    file.AddUsing("Microsoft.Extensions.DependencyInjection");

                    var @class = file.Classes.First();
                    @class.ImplementsInterface(this.GetIDispatcherName());

                    @class.AddField(
                        UseType("System.Collections.Concurrent.ConcurrentDictionary<System.Type, object>"),
                        "Invokers",
                        field => field.PrivateReadOnly().Static().WithAssignment(new CSharpStatement("new()")));

                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter(UseType("System.IServiceProvider"), "serviceProvider", parameter =>
                            parameter.IntroduceReadonlyField(field => field.BeforeSeparator = CSharpCodeSeparatorType.EmptyLines));
                    });

                    @class.AddMethod($"{UseType("System.Threading.Tasks.Task")}<TResponse>", "Send", method =>
                    {
                        method.AddGenericParameter("TResponse");
                        method.AddParameter($"{this.GetIAppRequestName()}<TResponse>", "request");
                        method.AddParameter(UseType("System.Threading.CancellationToken"), "cancellationToken", parameter =>
                            parameter.WithDefaultValue("default"));

                        method.AddStatement($"{UseType("System.ArgumentNullException")}.ThrowIfNull(request);");
                        method.AddStatement(
                            "var invoker = (HandlerInvoker<TResponse>)Invokers.GetOrAdd(request.GetType(), CreateInvoker<TResponse>);",
                            statement => statement.SeparatedFromPrevious());
                        method.AddStatement(
                            "return invoker.Invoke(request, _serviceProvider, cancellationToken);",
                            statement => statement.SeparatedFromPrevious());
                    });

                    @class.AddMethod("object", "CreateInvoker", method =>
                    {
                        method.Private().Static();
                        method.AddGenericParameter("TResponse");
                        method.AddParameter(UseType("System.Type"), "requestType");

                        method.AddStatement("var invokerType = typeof(HandlerInvoker<,>).MakeGenericType(requestType, typeof(TResponse));");
                        method.AddStatement($"return {UseType("System.Activator")}.CreateInstance(invokerType)!;");
                    });

                    @class.AddNestedClass("HandlerInvoker", nested =>
                    {
                        nested.Private().Abstract();
                        nested.AddGenericParameter("TResponse");
                        nested.AddMethod($"{UseType("System.Threading.Tasks.Task")}<TResponse>", "Invoke", method =>
                        {
                            method.Abstract();
                            method.AddParameter($"{this.GetIAppRequestName()}<TResponse>", "request");
                            method.AddParameter(UseType("System.IServiceProvider"), "serviceProvider");
                            method.AddParameter(UseType("System.Threading.CancellationToken"), "cancellationToken");
                        });
                    });

                    @class.AddNestedClass("HandlerInvoker", nested =>
                    {
                        nested.Private().Sealed();
                        nested.BeforeSeparator = CSharpCodeSeparatorType.EmptyLines;
                        nested.AddGenericParameter("TRequest");
                        nested.AddGenericParameter("TResponse");
                        nested.WithBaseType("HandlerInvoker<TResponse>");
                        nested.AddGenericTypeConstraint("TRequest", constraint =>
                            constraint.AddType($"{this.GetIAppRequestName()}<TResponse>"));
                        nested.AddMethod($"{UseType("System.Threading.Tasks.Task")}<TResponse>", "Invoke", method =>
                        {
                            method.Override();
                            method.AddParameter($"{this.GetIAppRequestName()}<TResponse>", "request");
                            method.AddParameter(UseType("System.IServiceProvider"), "serviceProvider");
                            method.AddParameter(UseType("System.Threading.CancellationToken"), "cancellationToken");

                            method.AddStatement($"var handler = serviceProvider.GetRequiredService<{this.GetIAppRequestHandlerName()}<TRequest, TResponse>>();");
                            method.AddStatement("return handler.Handle((TRequest)request, cancellationToken);");
                        });
                    });
                }, 0);
        }

        [IntentManaged(Mode.Ignore)]
        public override void BeforeTemplateExecution()
        {
            base.BeforeTemplateExecution();

            ExecutionContext.EventDispatcher.Publish(ContainerRegistrationRequest
                .ToRegister(this)
                .ForInterface(GetTemplate<IClassProvider>(IDispatcherTemplate.TemplateId))
                .ForConcern("Application")
                .WithPerServiceCallLifeTime()
                .WithPriority(10));
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
