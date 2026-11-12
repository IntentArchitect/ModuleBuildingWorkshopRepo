using System.Collections.Concurrent;
using Intent.RoslynWeaver.Attributes;
using Microsoft.Extensions.DependencyInjection;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.Dispatcher", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public class Dispatcher : IDispatcher
    {
        private static readonly ConcurrentDictionary<Type, object> Invokers = new();

        private readonly IServiceProvider _serviceProvider;

        public Dispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<TResponse> Send<TResponse>(
            IAppRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var invoker = (HandlerInvoker<TResponse>)Invokers.GetOrAdd(request.GetType(), CreateInvoker<TResponse>);
            return invoker.Invoke(request, _serviceProvider, cancellationToken);
        }

        private static object CreateInvoker<TResponse>(Type requestType)
        {
            var invokerType = typeof(HandlerInvoker<,>).MakeGenericType(requestType, typeof(TResponse));
            return Activator.CreateInstance(invokerType)!;
        }

        private abstract class HandlerInvoker<TResponse>
        {
            public abstract Task<TResponse> Invoke(
                IAppRequest<TResponse> request,
                IServiceProvider serviceProvider,
                CancellationToken cancellationToken);
        }

        private sealed class HandlerInvoker<TRequest, TResponse> : HandlerInvoker<TResponse>
            where TRequest : IAppRequest<TResponse>
        {
            public override Task<TResponse> Invoke(
                IAppRequest<TResponse> request,
                IServiceProvider serviceProvider,
                CancellationToken cancellationToken)
            {
                var handler = serviceProvider.GetRequiredService<IAppRequestHandler<TRequest, TResponse>>();
                return handler.Handle((TRequest)request, cancellationToken);
            }
        }
    }
}
