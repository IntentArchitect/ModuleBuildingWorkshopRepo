using ConsumerTestApp.Application.Common.Interfaces;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.CorrelationDelegatingHandler", Version = "1.0")]

namespace ConsumerTestApp.Infrastructure.HttpClients
{
    public class CorrelationDelegatingHandler : DelegatingHandler
    {
        private const string HeaderName = "X-Correlation-Id";

        private readonly ICorrelationContext _correlationContext;

        public CorrelationDelegatingHandler(ICorrelationContext correlationContext)
        {
            _correlationContext = correlationContext;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationId = _correlationContext.CorrelationId;
            if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(HeaderName))
            {
                request.Headers.TryAddWithoutValidation(HeaderName, correlationId);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
