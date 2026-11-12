using System.Diagnostics;
using ConsumerTestApp.Application.Common.Dispatching;
using Microsoft.Extensions.Logging;

namespace ConsumerTestApp.Application.Commands.Products
{
    public class CreateProductCommandLoggingDecorator : IAppRequestHandler<CreateProductCommand, Guid>
    {
        private readonly IAppRequestHandler<CreateProductCommand, Guid> _inner;
        private readonly ILogger<CreateProductCommandLoggingDecorator> _logger;

        public CreateProductCommandLoggingDecorator(IAppRequestHandler<CreateProductCommand, Guid> inner,
            ILogger<CreateProductCommandLoggingDecorator> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling {RequestName} {@Request}", nameof(CreateProductCommand), request);

            var stopwatch = Stopwatch.StartNew();
            object? response = null;
            try
            {
                var result = await _inner.Handle(request, cancellationToken);
                response = result;
                return result;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "Handled {RequestName} with response {@Response} in {ElapsedMilliseconds}ms",
                    nameof(CreateProductCommand),
                    response,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
