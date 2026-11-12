using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace WorkshopModule.Templates.CommandRequest
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class CommandRequestTemplate : CSharpTemplateBase<CommandModel>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "WorkshopModule.CommandRequest";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public CommandRequestTemplate(IOutputTarget outputTarget, CommandModel model) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddClass(Model.Name)
                .OnBuild(file =>
                {
                    var response = Model.TypeReference.Element != null
                        ? GetTypeName(Model.TypeReference)
                        : "Unit";

                    var @class = file.Classes.First();
                    @class.ImplementsInterface($"{this.GetIAppRequestName()}<{response}>");
                    @class.AddConstructor(ctor =>
                    {
                        foreach (var field in Model.Properties)
                        {
                            ctor.AddParameter(GetTypeName(field.TypeReference), field.Name.ToParameterName(), parameter =>
                                parameter.IntroduceProperty());
                        }
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
