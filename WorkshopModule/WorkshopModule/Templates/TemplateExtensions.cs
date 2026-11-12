using System.Collections.Generic;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using WorkshopModule.Templates.CommandHandler;
using WorkshopModule.Templates.CommandRequest;
using WorkshopModule.Templates.CorrelationContext;
using WorkshopModule.Templates.CorrelationDelegatingHandler;
using WorkshopModule.Templates.CorrelationMiddleware;
using WorkshopModule.Templates.Dispatcher;
using WorkshopModule.Templates.IAppRequest;
using WorkshopModule.Templates.IAppRequestHandler;
using WorkshopModule.Templates.ICorrelationContext;
using WorkshopModule.Templates.IDispatcher;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: DefaultIntentManaged(Mode.Fully, Targets = Targets.Usings)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.TemplateExtensions", Version = "1.0")]

namespace WorkshopModule.Templates
{
    public static class TemplateExtensions
    {
        public static string GetCommandHandlerName<T>(this IIntentTemplate<T> template) where T : CommandModel
        {
            return template.GetTypeName(CommandHandlerTemplate.TemplateId, template.Model);
        }

        public static string GetCommandHandlerName(this IIntentTemplate template, CommandModel model)
        {
            return template.GetTypeName(CommandHandlerTemplate.TemplateId, model);
        }

        public static string GetCommandRequestName<T>(this IIntentTemplate<T> template) where T : CommandModel
        {
            return template.GetTypeName(CommandRequestTemplate.TemplateId, template.Model);
        }

        public static string GetCommandRequestName(this IIntentTemplate template, CommandModel model)
        {
            return template.GetTypeName(CommandRequestTemplate.TemplateId, model);
        }

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

        public static string GetDispatcherName(this IIntentTemplate template)
        {
            return template.GetTypeName(DispatcherTemplate.TemplateId);
        }

        public static string GetIAppRequestName(this IIntentTemplate template)
        {
            return template.GetTypeName(IAppRequestTemplate.TemplateId);
        }

        public static string GetIAppRequestHandlerName(this IIntentTemplate template)
        {
            return template.GetTypeName(IAppRequestHandlerTemplate.TemplateId);
        }

        public static string GetICorrelationContextName(this IIntentTemplate template)
        {
            return template.GetTypeName(ICorrelationContextTemplate.TemplateId);
        }

        public static string GetIDispatcherName(this IIntentTemplate template)
        {
            return template.GetTypeName(IDispatcherTemplate.TemplateId);
        }

    }
}