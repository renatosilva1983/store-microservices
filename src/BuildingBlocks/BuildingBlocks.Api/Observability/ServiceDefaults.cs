using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Api.Observability;

/// <summary>
/// "Service defaults": o que toda API do sistema tem igual, num lugar só
/// (mesma ideia do ServiceDefaults do .NET Aspire): ProblemDetails, health check,
/// correlation id e formato de log.
/// </summary>
public static class ServiceDefaults
{
    public const string HealthEndpoint = "/health";

    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        // Nenhum serviço anuncia a tecnologia no header "Server" (OWASP A05).
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
        builder.Services.ConfigureHttpClientDefaults(http => http.AddHttpMessageHandler<CorrelationIdDelegatingHandler>());

        // Desenvolvimento: log legível no console. Container/produção: JSON estruturado
        // (CloudWatch, Seq, Elastic leem os campos, inclusive o CorrelationId do escopo).
        builder.Logging.ClearProviders();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
        }
        else
        {
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
            });
        }

        return builder;
    }

    /// <summary>Correlation Id no começo do pipeline, para valer em tudo que vem depois.</summary>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }

    public static WebApplication MapServiceDefaults(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpoint);
        return app;
    }
}
