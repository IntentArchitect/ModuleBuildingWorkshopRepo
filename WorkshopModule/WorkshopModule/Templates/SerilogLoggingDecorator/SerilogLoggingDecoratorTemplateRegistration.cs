using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modelers.Services.Api;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.Modules.Common.Registrations;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;
using WorkshopModule.Api;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.TemplateRegistration.FilePerModel", Version = "1.0")]

namespace WorkshopModule.Templates.SerilogLoggingDecorator
{
    [IntentManaged(Mode.Merge, Body = Mode.Merge, Signature = Mode.Fully)]
    public class SerilogLoggingDecoratorTemplateRegistration : FilePerModelTemplateRegistration<CommandModel>
    {
        private readonly IMetadataManager _metadataManager;

        public SerilogLoggingDecoratorTemplateRegistration(IMetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
        }

        public override string TemplateId => SerilogLoggingDecoratorTemplate.TemplateId;

        [IntentManaged(Mode.Fully)]
        public override ITemplate CreateTemplateInstance(IOutputTarget outputTarget, CommandModel model)
        {
            return new SerilogLoggingDecoratorTemplate(outputTarget, model);
        }

        [IntentManaged(Mode.Merge, Body = Mode.Ignore, Signature = Mode.Fully)]
        public override IEnumerable<CommandModel> GetModels(IApplication application)
        {
            return _metadataManager.Services(application).GetCommandModels().Where(x => x.HasSerilogLogging()).ToList();
        }
    }
}
