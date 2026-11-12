using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.IDispatcher", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public interface IDispatcher
    {
        Task<TResponse> Send<TResponse>(IAppRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}
