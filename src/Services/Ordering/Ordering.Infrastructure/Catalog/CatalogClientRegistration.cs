using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ordering.Application.Abstractions;

namespace Ordering.Infrastructure.Catalog;

internal static class CatalogClientRegistration
{
    public static IServiceCollection AddCatalogClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CatalogServiceOptions>()
            .Bind(configuration.GetSection(CatalogServiceOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                $"'{CatalogServiceOptions.SectionName}:BaseUrl' must be an absolute URL.")
            .ValidateOnStart();

        services.AddHttpClient<IProductCatalog, CatalogHttpAdapter>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<CatalogServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            // Pipeline de resiliência (Polly): limite total → retry → circuit breaker → timeout por tentativa.
            // Valores reduzidos em relação ao padrão para ver o circuito abrir num teste manual.
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.Retry.MaxRetryAttempts = 2;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });

        return services;
    }
}
