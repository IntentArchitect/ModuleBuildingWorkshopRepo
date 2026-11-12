using ConsumerTestApp.Application.Common.Interfaces;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.CorrelationContext", Version = "1.0")]

namespace ConsumerTestApp.Infrastructure.Configuration.Correlation
{
    public class CorrelationContext : ICorrelationContext
    {
        public string? CorrelationId { get; private set; }

        public void SetCorrelationId(string correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
