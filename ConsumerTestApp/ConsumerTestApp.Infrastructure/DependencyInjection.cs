using ConsumerTestApp.Application.Common.Interfaces;
using ConsumerTestApp.Domain.Common.Interfaces;
using ConsumerTestApp.Infrastructure.Configuration.Correlation;
using ConsumerTestApp.Infrastructure.HttpClients;
using ConsumerTestApp.Infrastructure.Persistence;
using Intent.RoslynWeaver.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.Infrastructure.DependencyInjection.DependencyInjection", Version = "1.0")]

namespace ConsumerTestApp.Infrastructure
{
    public static class DependencyInjection
    {
        [IntentMerge]
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("DefaultConnection");
                options.UseLazyLoadingProxies();
            });
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<ICorrelationContext, CorrelationContext>();
            services.AddTransient<CorrelationDelegatingHandler>();
            return services;
        }
    }
}