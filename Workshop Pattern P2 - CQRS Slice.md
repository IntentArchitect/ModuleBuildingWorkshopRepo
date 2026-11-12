Without using Intent, hand-write a minimal in-memory CQRS slice in ConsumerTestApp, the way you would if MediatR didn't exist. Use exactly these paths, and register everything in the existing Application DependencyInjection.cs:
- ConsumerTestApp.Application/Common/Dispatching/IAppRequest.cs — IAppRequest<out TResponse>, plus a Unit record (a record, not a struct) for requests with no result
- ConsumerTestApp.Application/Common/Dispatching/IAppRequestHandler.cs — IAppRequestHandler<in TRequest, TResponse> where TRequest : IAppRequest<TResponse>, with Handle(TRequest, CancellationToken)
- ConsumerTestApp.Application/Common/Dispatching/IDispatcher.cs — Send<TResponse>(IAppRequest<TResponse>, CancellationToken)
- ConsumerTestApp.Application/Common/Dispatching/Dispatcher.cs — resolves the matching handler from IServiceProvider; registered scoped against IDispatcher
- ConsumerTestApp.Application/Commands/Products/CreateProductCommand.cs — Title (string), Price (decimal), returns Guid
- ConsumerTestApp.Application/Commands/Products/CreateProductCommandHandler.cs — Handle throws NotImplementedException; registered scoped against IAppRequestHandler<CreateProductCommand, Guid>
- ConsumerTestApp.Application/Commands/Products/CreateProductCommandLoggingDecorator.cs — implements the same handler interface, wraps the inner handler with an ILogger and a Stopwatch in try/finally, logging the request, the response and the duration; registered with Scrutor 7.0.0's services.Decorate after the handler registration
- ConsumerTestApp.Api/Controllers/ProductsController.cs — POST api/products, injects IDispatcher, does var result = await _dispatcher.Send(command, cancellationToken); and return Created(string.Empty, result);

Match these shapes exactly:
- Unit is public sealed record Unit with public static Unit Value { get; } = new(); (a property, not a field).
- IDispatcher.Send takes CancellationToken cancellationToken = default.
- Dispatcher caches one handler invoker per request type in a static ConcurrentDictionary<Type, object>, created by a private static object CreateInvoker<TResponse>(Type requestType) helper (no lambda), using nested HandlerInvoker<TResponse> and HandlerInvoker<TRequest, TResponse> classes. Any signature that doesn't fit on one line puts each parameter on its own line.
- CreateProductCommand has a constructor (string title, decimal price) and get/set properties. CreateProductCommandHandler has an empty constructor, and Handle throws new NotImplementedException("Implement CreateProductCommandHandler.Handle").
- The decorator's constructor keeps its first parameter on the opening line. Handle logs "Handling {RequestName} {@Request}" before, stores the response as object? response, and in finally logs "Handled {RequestName} with response {@Response} in {ElapsedMilliseconds}ms".
- ProductsController has no class-level route: the action carries [HttpPost("api/products")], takes [FromBody] CreateProductCommand command and CancellationToken cancellationToken = default.
- Add the Scrutor 7.0.0 and Microsoft.Extensions.Logging.Abstractions packages to the Application project.

Don't model anything in the Services designer, don't add Intent code-management attributes, and don't run the Software Factory. Build the solution when you're done.
