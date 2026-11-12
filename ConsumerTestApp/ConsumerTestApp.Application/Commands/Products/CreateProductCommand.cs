using ConsumerTestApp.Application.Common.Dispatching;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("WorkshopModule.CommandRequest", Version = "1.0")]

namespace ConsumerTestApp.Application.Commands.Products
{
    public class CreateProductCommand : IAppRequest<Guid>
    {
        public CreateProductCommand(string title, decimal price)
        {
            Title = title;
            Price = price;
        }

        public string Title { get; set; }
        public decimal Price { get; set; }
    }
}
