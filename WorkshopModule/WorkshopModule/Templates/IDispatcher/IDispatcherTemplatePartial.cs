using System;
using System.Collections.Generic;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.IDispatcher
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class IDispatcherTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.IDispatcher";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public IDispatcherTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddInterface("IDispatcher")
                .OnBuild(file =>
                {
                    file.Interfaces[0].AddMethod($"{UseType("System.Threading.Tasks.Task")}<TResponse>", "Send", method =>
                    {
                        method.AddGenericParameter("TResponse");
                        method.AddParameter($"{this.GetIAppRequestName()}<TResponse>", "request");
                        method.AddParameter(UseType("System.Threading.CancellationToken"), "cancellationToken", parameter =>
                            parameter.WithDefaultValue("default"));
                    });
                }, 0);
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
