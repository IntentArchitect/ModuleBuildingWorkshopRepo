using System.Reflection;
using ConsumerTestApp.Application.Commands.Products;
using ConsumerTestApp.Application.Common.Dispatching;
using ConsumerTestApp.Application.Common.Validation;
using FluentValidation;
using Intent.RoslynWeaver.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.Application.DependencyInjection.DependencyInjection", Version = "1.0")]

namespace ConsumerTestApp.Application
{
    public static class DependencyInjection
    {
        [IntentMerge]
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), lifetime: ServiceLifetime.Transient);
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddScoped<IValidatorProvider, ValidatorProvider>();
            services.AddScoped<IDispatcher, Dispatcher>();
            services.AddScoped<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandHandler>();
            services.Decorate<IAppRequestHandler<CreateProductCommand, Guid>, CreateProductCommandLoggingDecorator>();
            return services;
        }
    }
}
