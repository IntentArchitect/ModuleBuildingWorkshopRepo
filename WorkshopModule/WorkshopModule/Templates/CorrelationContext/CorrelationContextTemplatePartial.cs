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
using WorkshopModule.Templates.ICorrelationContext;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.CorrelationContext
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class CorrelationContextTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.CorrelationContext";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public CorrelationContextTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddClass("CorrelationContext")
                .OnBuild(file =>
                {
                    var @class = file.Classes.First();

                    @class.ImplementsInterface(this.GetICorrelationContextName());
                    @class.AddProperty("string?", "CorrelationId", property => property.PrivateSetter());
                    @class.AddMethod("void", "SetCorrelationId", method =>
                    {
                        method.AddParameter("string", "correlationId");
                        method.AddStatement("CorrelationId = correlationId;");
                    });
                }, 0);
        }

        [IntentManaged(Mode.Ignore)]
        public override void BeforeTemplateExecution()
        {
            base.BeforeTemplateExecution();

            ExecutionContext.EventDispatcher.Publish(ContainerRegistrationRequest
                .ToRegister(this)
                .ForInterface(GetTemplate<IClassProvider>(ICorrelationContextTemplate.TemplateId))
                .ForConcern("Infrastructure")
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
