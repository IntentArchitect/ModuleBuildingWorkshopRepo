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
using WorkshopModule.Settings;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.CorrelationMiddleware
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class CorrelationMiddlewareTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.CorrelationMiddleware";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public CorrelationMiddlewareTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddClass("CorrelationMiddleware")
                .AddClass("CorrelationMiddlewareExtensions")
                .OnBuild(file =>
                {
                    var settings = ExecutionContext.Settings.GetTracingSettings();
                    var headerName = string.IsNullOrWhiteSpace(settings.CorrelationHeaderName())
                        ? "X-Correlation-Id"
                        : settings.CorrelationHeaderName();
                    var echoToResponse = settings.EchoToResponse();

                    var middleware = file.Classes.First(c => c.Name == "CorrelationMiddleware");
                    var extensions = file.Classes.First(c => c.Name == "CorrelationMiddlewareExtensions");

                    middleware.AddField("string", "HeaderName", field => field.Constant($"\"{headerName}\""));
                    middleware.AddConstructor(ctor =>
                    {
                        ctor.AddParameter(UseType("Microsoft.AspNetCore.Http.RequestDelegate"), "next", parameter =>
                            parameter.IntroduceReadonlyField(field => field.BeforeSeparator = CSharpCodeSeparatorType.EmptyLines));
                        ctor.AddParameter(UseType($"Microsoft.Extensions.Logging.ILogger<{ClassName}>"), "logger", parameter =>
                            parameter.IntroduceReadonlyField());
                    });
                    middleware.AddMethod(UseType("System.Threading.Tasks.Task"), "InvokeAsync", method =>
                    {
                        method.Async();
                        method.AddParameter(UseType("Microsoft.AspNetCore.Http.HttpContext"), "context");
                        method.AddParameter(this.GetICorrelationContextName(), "correlationContext");

                        method.AddStatement(
                            "var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values) && !string.IsNullOrWhiteSpace(values.ToString())\n" +
                            "    ? values.ToString()\n" +
                            $"    : {UseType("System.Guid")}.NewGuid().ToString();");

                        method.AddStatement("correlationContext.SetCorrelationId(correlationId);", statement => statement.SeparatedFromPrevious());

                        if (echoToResponse)
                        {
                            method.AddInvocationStatement("context.Response.OnStarting", invocation => invocation
                                .AddArgument(new CSharpLambdaBlock("()")
                                    .AddStatement("context.Response.Headers[HeaderName] = correlationId;")
                                    .AddStatement($"return {UseType("System.Threading.Tasks.Task")}.CompletedTask;"))
                                .SeparatedFromPrevious());
                        }

                        method.AddStatement(
                            new CSharpAssignmentStatement(
                                new CSharpVariableDeclaration("scopeState"),
                                new CSharpObjectInitializerBlock($"new {UseType("System.Collections.Generic.Dictionary<string, object>")}")
                                    .AddInitStatement("[\"CorrelationId\"]", "correlationId")
                                    .WithSemicolon()),
                            statement => statement.SeparatedFromPrevious());

                        method.AddUsingBlock("_logger.BeginScope(scopeState)", block =>
                            block.AddStatement("await _next(context);"));
                    });

                    extensions.Static();
                    extensions.AddMethod(UseType("Microsoft.AspNetCore.Builder.IApplicationBuilder"), "UseCorrelationId", method =>
                    {
                        method.Static();
                        method.AddParameter(UseType("Microsoft.AspNetCore.Builder.IApplicationBuilder"), "app", parameter => parameter.WithThisModifier());
                        method.AddStatement($"return app.UseMiddleware<{ClassName}>();");
                    });
                }, 0);
        }

        [IntentManaged(Mode.Ignore)]
        public override void BeforeTemplateExecution()
        {
            base.BeforeTemplateExecution();

            ExecutionContext.EventDispatcher.Publish(ApplicationBuilderRegistrationRequest
                .ToRegister("UseCorrelationId")
                .HasDependency(this)
                .WithPriority(-1000));
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
