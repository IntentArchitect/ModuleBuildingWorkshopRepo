using System.Collections.Generic;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using WorkshopModule.Templates.CorrelationContext;
using WorkshopModule.Templates.CorrelationDelegatingHandler;
using WorkshopModule.Templates.CorrelationMiddleware;
using WorkshopModule.Templates.ICorrelationContext;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace WorkshopModule.Templates
{
    public static class TemplateExtensions
    {
        public static string GetCorrelationContextName(this IIntentTemplate template)
        {
            return template.GetTypeName(CorrelationContextTemplate.TemplateId);
        }

        public static string GetCorrelationDelegatingHandlerName(this IIntentTemplate template)
        {
            return template.GetTypeName(CorrelationDelegatingHandlerTemplate.TemplateId);
        }

        public static string GetCorrelationMiddlewareName(this IIntentTemplate template)
        {
            return template.GetTypeName(CorrelationMiddlewareTemplate.TemplateId);
        }

        public static string GetICorrelationContextName(this IIntentTemplate template)
        {
            return template.GetTypeName(ICorrelationContextTemplate.TemplateId);
        }

    }
}