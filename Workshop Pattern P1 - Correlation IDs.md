Without using Intent, hand-write correlation ID support in ConsumerTestApp:
- An ICorrelationContext interface in the Application layer (alongside the existing ICurrentUser), with a CorrelationContext implementation in Infrastructure, registered as scoped against ICorrelationContext.
- A CorrelationMiddleware that reads the X-Correlation-Id header (or generates a new GUID), stores it on ICorrelationContext injected into InvokeAsync (not the constructor), wraps the rest of the pipeline in an ILogger.BeginScope (not Serilog's LogContext), and echoes the header on the response via Response.OnStarting. Add a UseCorrelationId() extension method and call it near the top of the pipeline in Program.cs.
- A CorrelationDelegatingHandler that takes ICorrelationContext in its constructor and adds the header to outgoing requests when a correlation ID is set, registered as transient.

Use exactly these paths, and register everything in the existing DependencyInjection.cs and Program.cs:
- ConsumerTestApp.Application/Common/Interfaces/ICorrelationContext.cs
- ConsumerTestApp.Infrastructure/Configuration/Correlation/CorrelationContext.cs
- ConsumerTestApp.Api/Middleware/CorrelationMiddleware.cs
- ConsumerTestApp.Infrastructure/HttpClients/CorrelationDelegatingHandler.cs

Match these shapes exactly:
- ICorrelationContext has string? CorrelationId { get; } and void SetCorrelationId(string correlationId). CorrelationContext implements it with a private setter.
- CorrelationMiddleware has public const string HeaderName = "X-Correlation-Id". Build the log scope as a separate variable, var scopeState = new Dictionary<string, object> { ["CorrelationId"] = correlationId };, then using (_logger.BeginScope(scopeState)). Put UseCorrelationId() in a static CorrelationMiddlewareExtensions class in the same file.
- CorrelationDelegatingHandler has private const string HeaderName = "X-Correlation-Id", and skips requests that already carry the header.

Do not add // IntentIgnore or any other Intent code-management attributes, and do not run the Software Factory. Build the solution when you're done.
