using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModelExtensions", Version = "1.0")]

namespace WorkshopModule.Api
{
    public static class CommandModelStereotypeExtensions
    {
        public static SerilogLogging GetSerilogLogging(this CommandModel model)
        {
            var stereotype = model.GetStereotype(SerilogLogging.DefinitionId);
            return stereotype != null ? new SerilogLogging(stereotype) : null;
        }


        public static bool HasSerilogLogging(this CommandModel model)
        {
            return model.HasStereotype(SerilogLogging.DefinitionId);
        }

        public static bool TryGetSerilogLogging(this CommandModel model, out SerilogLogging stereotype)
        {
            if (!HasSerilogLogging(model))
            {
                stereotype = null;
                return false;
            }

            stereotype = new SerilogLogging(model.GetStereotype(SerilogLogging.DefinitionId));
            return true;
        }

        public class SerilogLogging
        {
            private IStereotype _stereotype;
            public const string DefinitionId = "c5d53e03-6048-4150-9731-e729f4d6843d";

            public SerilogLogging(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

            public bool LogRequest()
            {
                return _stereotype.GetProperty<bool>("Log Request");
            }

            public bool LogResponse()
            {
                return _stereotype.GetProperty<bool>("Log Response");
            }

            public bool LogDuration()
            {
                return _stereotype.GetProperty<bool>("Log Duration");
            }

        }

    }
}