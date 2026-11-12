using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.IAppRequestHandler", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public interface IAppRequestHandler<in TRequest, TResponse>
        where TRequest : IAppRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
