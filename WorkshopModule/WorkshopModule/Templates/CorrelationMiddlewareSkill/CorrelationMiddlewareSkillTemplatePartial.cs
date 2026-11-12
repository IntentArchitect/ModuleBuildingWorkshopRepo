using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Engine;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.Modules.Common.FileBuilders.MarkdownFileBuilder;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;
using WorkshopModule.Settings;
using WorkshopModule.Templates.ICorrelationContext;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.ProjectItemTemplate.Partial", Version = "1.0")]

namespace WorkshopModule.Templates.CorrelationMiddlewareSkill
{
    [IntentManaged(Mode.Merge, Signature = Mode.Fully)]
    public class CorrelationMiddlewareSkillTemplate : MarkdownBaseTemplate<object>, IMarkdownFileBuilderTemplate
    {
        [IntentManaged(Mode.Fully)]
        public const string TemplateId = "WorkshopModule.CorrelationMiddlewareSkill";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public CorrelationMiddlewareSkillTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            WithContentHashing = false;
            MarkdownFile = new MarkdownFile("SKILL", relativeLocation: "custom-dispatcher-middleware")
                .FromMarkdown("""
---
name: custom-dispatcher-middleware
description: Rules for getting and propagating the request correlation ID through ICorrelationContext, CorrelationMiddleware and CorrelationDelegatingHandler. MUST load before writing code that reads, logs, forwards or creates a correlation ID, or before editing middleware, the request pipeline or outbound HttpClient setup.
---
# Correlation ID

The ID is read from the incoming header, or created, in exactly one place: `CorrelationMiddleware`, which runs first in the pipeline through `app.UseCorrelationId()`. The middleware then:

- stores the ID in the scoped `ICorrelationContext`;
- echoes it on the response;
- opens a logging scope that carries `CorrelationId`.

All other code gets the ID from there. WorkshopModule generates the interface, its implementation, the middleware and the delegating handler.

## Reading it

- Inject `ICorrelationContext` and read `CorrelationId`.
- Never read it from HTTP headers. That rules out `Request.Headers["X-Correlation-Id"]`, `CorrelationMiddleware.HeaderName` and `IHttpContextAccessor`. Never use `HttpContext.TraceIdentifier`, `Activity.Current` or the trace ID in its place.
- Never generate one. Don't fall back to `Guid.NewGuid()` when it's null, and never call `SetCorrelationId`; only the middleware does. Outside an HTTP request `CorrelationId` is null, and null means "no correlation ID", not "make one up".
- `ICorrelationContext` is scoped. Don't inject it into singletons or capture it in background work. Read the value inside the request and pass the string along.

## Usually you don't need it

- **Logs:** everything logged during the request already carries `CorrelationId` from the middleware's scope. Don't add it to message templates, and don't open another `BeginScope` for it.
- **Outgoing HTTP calls:** `CorrelationDelegatingHandler` copies the ID onto the request. Attach it where the client is registered, with `.AddHttpMessageHandler<CorrelationDelegatingHandler>()`. Never set the header yourself.
- **Command handlers:** don't inject `ICorrelationContext` at all, because handlers depend on Domain abstractions only (see `custom-dispatcher-command-handler`).

## Changing the behaviour

- The header name and whether the ID is echoed on the response are the **Tracing Settings** application settings, `Correlation Header Name` and `Echo To Response`. Change them there and run the Software Factory.
- Never hardcode `"X-Correlation-Id"`, and never edit `CorrelationMiddleware`, `CorrelationContext` or `CorrelationDelegatingHandler`.
- Don't move or duplicate `app.UseCorrelationId()`. It has to run before any request-logging middleware, such as Serilog's `UseSerilogRequestLogging()`, or the request logs lose the ID.
""")
                .OnBuild(file =>
                {
                    var settings = ExecutionContext.Settings.GetTracingSettings();
                    var header = string.IsNullOrWhiteSpace(settings.CorrelationHeaderName()) ? "X-Correlation-Id" : settings.CorrelationHeaderName();
                    var contextNamespace = (ExecutionContext.FindTemplateInstance<ICorrelationContextTemplate>(ICorrelationContextTemplate.TemplateId)
                        ?? throw new InvalidOperationException($"Template {ICorrelationContextTemplate.TemplateId} not found")).Namespace;

                    if (!settings.EchoToResponse())
                    {
                        file.ConfigureSection("Correlation ID", s => s.RemoveListItem(x => x.Content == "echoes it on the response;"));
                    }

                    file.ConfigureSection("Reading it", section =>
                    {
                        section.ListItems.Single(x => x.Content.StartsWith("Inject `ICorrelationContext`")).Content =
                            $"Inject `ICorrelationContext` (from `{contextNamespace}`) and read `CorrelationId`.";
                        section.ListItems.Single(x => x.Content.StartsWith("Never read it from HTTP headers.")).Content =
                            $"Never read it from HTTP headers. That rules out `Request.Headers[\"{header}\"]`, `CorrelationMiddleware.HeaderName` and `IHttpContextAccessor`. Never use `HttpContext.TraceIdentifier`, `Activity.Current` or the trace ID in its place.";
                    });
                    file.ConfigureSection("Changing the behaviour", section =>
                    {
                        section.ListItems.Single(x => x.Content.StartsWith("Never hardcode ")).Content =
                            $"Never hardcode `\"{header}\"`, and never edit `CorrelationMiddleware`, `CorrelationContext` or `CorrelationDelegatingHandler`.";
                    });
                }, 0);
        }

        [IntentManaged(Mode.Fully)]
        public override IMarkdownFile MarkdownFile { get; }

        [IntentManaged(Mode.Fully)]
        public override ITemplateFileConfig GetTemplateFileConfig() => MarkdownFile.GetConfig();

    }
}
