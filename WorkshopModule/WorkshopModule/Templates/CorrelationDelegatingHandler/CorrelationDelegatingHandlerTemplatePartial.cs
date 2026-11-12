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

namespace WorkshopModule.Templates.CorrelationDelegatingHandler
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class CorrelationDelegatingHandlerTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.CorrelationDelegatingHandler";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public CorrelationDelegatingHandlerTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddClass("CorrelationDelegatingHandler")
                .OnBuild(file =>
                {
                    var settings = ExecutionContext.Settings.GetTracingSettings();
                    var headerName = string.IsNullOrWhiteSpace(settings.CorrelationHeaderName())
                        ? "X-Correlation-Id"
                        : settings.CorrelationHeaderName();

                    var @class = file.Classes.First();

                    @class.WithBaseType(UseType("System.Net.Http.DelegatingHandler"));
                    @class.AddField("string", "HeaderName", field => field.PrivateConstant($"\"{headerName}\""));
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter(this.GetICorrelationContextName(), "correlationContext", parameter =>
                            parameter.IntroduceReadonlyField(field => field.BeforeSeparator = CSharpCodeSeparatorType.EmptyLines));
                    });
                    @class.AddMethod($"{UseType("System.Threading.Tasks.Task")}<{UseType("System.Net.Http.HttpResponseMessage")}>", "SendAsync", method =>
                    {
                        method.Protected().Override();
                        method.AddParameter(UseType("System.Net.Http.HttpRequestMessage"), "request");
                        method.AddParameter(UseType("System.Threading.CancellationToken"), "cancellationToken");

                        method.AddStatement("var correlationId = _correlationContext.CorrelationId;");
                        method.AddIfStatement("!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(HeaderName)", @if =>
                        {
                            @if.SeparatedFromPrevious(false);
                            @if.AddStatement("request.Headers.TryAddWithoutValidation(HeaderName, correlationId);");
                        });
                        method.AddStatement("return base.SendAsync(request, cancellationToken);", statement => statement.SeparatedFromPrevious());
                    });
                }, 0);
        }

        [IntentManaged(Mode.Ignore)]
        public override void BeforeTemplateExecution()
        {
            base.BeforeTemplateExecution();

            ExecutionContext.EventDispatcher.Publish(ContainerRegistrationRequest
                .ToRegister(this)
                .ForConcern("Infrastructure")
                .WithPriority(11));
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
