using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.ICorrelationContext", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Interfaces
{
    public interface ICorrelationContext
    {
        string? CorrelationId { get; }
        void SetCorrelationId(string correlationId);
    }
}
