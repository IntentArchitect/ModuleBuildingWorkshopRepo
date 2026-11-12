using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.ICorrelationContext
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class ICorrelationContextTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.ICorrelationContext";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public ICorrelationContextTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddInterface("ICorrelationContext")
                .OnBuild(file =>
                {
                    var @interface = file.Interfaces.First();

                    @interface.AddProperty("string?", "CorrelationId", property => property.WithoutSetter());
                    @interface.AddMethod("void", "SetCorrelationId", method =>
                    {
                        method.AddParameter("string", "correlationId");
                        method.BeforeSeparator = CSharpCodeSeparatorType.NewLine;
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
