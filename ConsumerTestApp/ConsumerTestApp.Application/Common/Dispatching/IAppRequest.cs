using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.IAppRequest", Version = "1.0")]

namespace ConsumerTestApp.Application.Common.Dispatching
{
    public interface IAppRequest<out TResponse>
    {
    }

    public sealed record Unit
    {
        public static Unit Value { get; } = new();
    }
}
