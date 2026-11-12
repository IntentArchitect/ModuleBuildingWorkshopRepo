using ConsumerTestApp.Application.Common.Dispatching;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.CommandHandler", Version = "1.0")]

namespace ConsumerTestApp.Application.Commands.Products
{
    public class CreateProductCommandHandler : IAppRequestHandler<CreateProductCommand, Guid>
    {
        public CreateProductCommandHandler()
        {
        }

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Implement CreateProductCommandHandler.Handle");
        }
    }
}
