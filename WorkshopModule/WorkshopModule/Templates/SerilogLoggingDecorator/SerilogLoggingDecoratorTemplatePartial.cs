using System;
using System.Collections.Generic;
using Intent.Engine;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.DependencyInjection;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;
using WorkshopModule.Api;
using WorkshopModule.Templates.IAppRequestHandler;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.SerilogLoggingDecorator
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SerilogLoggingDecoratorTemplate : CSharpTemplateBase<CommandModel>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.SerilogLoggingDecorator";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SerilogLoggingDecoratorTemplate(IOutputTarget outputTarget, CommandModel model) : base(TemplateId, outputTarget, model)
        {
            AddNugetDependency("Scrutor", "7.0.0");
            AddNugetDependency("Microsoft.Extensions.Logging.Abstractions", "10.0.9");

            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddClass($"{Model.Name}LoggingDecorator")
                .OnBuild(file =>
                {
                    var logRequest = false;
                    var logResponse = false;
                    var logDuration = false;
                    if (Model.TryGetSerilogLogging(out var logging))
                    {
                        logRequest = logging.LogRequest();
                        logResponse = logging.LogResponse();
                        logDuration = logging.LogDuration();
                    }

                    var response = Model.TypeReference.Element != null
                        ? GetTypeName(Model.TypeReference)
                        : "Unit";
                    var command = this.GetCommandRequestName(Model);
                    var handlerInterface = $"{this.GetIAppRequestHandlerName()}<{command}, {response}>";

                    var @class = file.Classes[0];
                    @class.ImplementsInterface(handlerInterface);
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter(handlerInterface, "inner", parameter => parameter.IntroduceReadonlyField());
                        ctor.AddParameter(UseType($"Microsoft.Extensions.Logging.ILogger<{ClassName}>"), "logger", parameter => parameter.IntroduceReadonlyField());
                    });
                    @class.AddMethod($"{UseType("System.Threading.Tasks.Task")}<{response}>", "Handle", method =>
                    {
                        method.Async();
                        method.AddParameter(command, "request");
                        method.AddParameter(UseType("System.Threading.CancellationToken"), "cancellationToken");

                        if (logRequest)
                        {
                            method.AddInvocationStatement("_logger.LogInformation", invocation => invocation
                                .AddArgument("\"Handling {RequestName} {@Request}\"")
                                .AddArgument($"nameof({command})")
                                .AddArgument("request"));
                        }

                        if (logDuration)
                        {
                            method.AddStatement($"var stopwatch = {UseType("System.Diagnostics.Stopwatch")}.StartNew();",
                                statement => statement.SeparatedFromPrevious());
                        }

                        if (logResponse)
                        {
                            method.AddStatement("object? response = null;",
                                statement =>
                                {
                                    if (!logDuration)
                                    {
                                        statement.SeparatedFromPrevious();
                                    }
                                });
                        }

                        if (!logResponse && !logDuration)
                        {
                            method.AddStatement("return await _inner.Handle(request, cancellationToken);",
                                statement => statement.SeparatedFromPrevious());
                            return;
                        }

                        method.AddTryBlock(@try =>
                        {
                            @try.SeparatedFromPrevious(false);
                            if (logResponse)
                            {
                                @try.AddStatement("var result = await _inner.Handle(request, cancellationToken);");
                                @try.AddStatement("response = result;");
                                @try.AddStatement("return result;");
                            }
                            else
                            {
                                @try.AddStatement("return await _inner.Handle(request, cancellationToken);");
                            }
                        });
                        method.AddFinallyBlock(@finally =>
                        {
                            if (logDuration)
                            {
                                @finally.AddStatement("stopwatch.Stop();");
                            }

                            var message = "Handled {RequestName}"
                                + (logResponse ? " with response {@Response}" : "")
                                + (logDuration ? " in {ElapsedMilliseconds}ms" : "");
                            @finally.AddInvocationStatement("_logger.LogInformation", invocation =>
                            {
                                invocation.WithArgumentsOnNewLines();
                                invocation.AddArgument($"\"{message}\"");
                                invocation.AddArgument($"nameof({command})");
                                if (logResponse)
                                {
                                    invocation.AddArgument("response");
                                }

                                if (logDuration)
                                {
                                    invocation.AddArgument("stopwatch.ElapsedMilliseconds");
                                }
                            });
                        });
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
            var handlerInterface = $"{this.GetIAppRequestHandlerName()}<{this.GetCommandRequestName(Model)}, {response}>";

            ExecutionContext.EventDispatcher.Publish(ServiceConfigurationRequest
                .ToRegister($"Decorate<{handlerInterface}, {ClassName}>")
                .ForConcern("Application")
                .RequiresUsingNamespaces(GetTemplate<IClassProvider>(IAppRequestHandlerTemplate.TemplateId).Namespace)
                .HasDependency(this)
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
