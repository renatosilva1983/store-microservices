using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace Store.Gateway.Security;

/// <summary>
/// Proteções de borda (ADR-41): como o Gateway é a única porta de entrada,
/// o que vale para todas as APIs é aplicado aqui, uma vez só.
/// - Rate limiting: limite de requisições por IP; no login, bem mais baixo (força bruta).
/// - Headers de segurança em todas as respostas e sem headers que revelam a tecnologia.
/// - HSTS fora de desenvolvimento.
/// </summary>
public static class GatewaySecurity
{
    /// <summary>Política do login: poucas tentativas por minuto por IP (OWASP API4 e A07).</summary>
    public const string LoginPolicy = "login";

    /// <summary>Política geral das APIs: protege contra consumo excessivo de recursos.</summary>
    public const string ApiPolicy = "api";

    public static IServiceCollection AddGatewaySecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(LoginPolicy, context => FixedWindowPerIp(context, limits.LoginPerMinute));
            options.AddPolicy(ApiPolicy, context => FixedWindowPerIp(context, limits.ApiPerMinute));

            // 429 com ProblemDetails e Retry-After: o cliente sabe quando tentar de novo.
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                var response = rejected.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "Too many requests",
                        status = StatusCodes.Status429TooManyRequests,
                        detail = "Limite de requisições atingido. Tente novamente em instantes."
                    },
                    cancellationToken);
            };
        });

        // Atrás de um load balancer (ALB na AWS), o IP do cliente chega no X-Forwarded-For.
        // Só é ligado por configuração: confiar nesse header sem um proxy na frente
        // deixaria qualquer um escolher o próprio IP e escapar do rate limiting.
        if (configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;

                // Na AWS só o ALB alcança o Gateway (security group), e o IP do ALB muda.
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        return services;
    }

    public static WebApplication UseGatewaySecurity(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        {
            app.UseForwardedHeaders();
        }

        // HSTS: o navegador passa a usar só HTTPS neste domínio. Fora de dev, porque
        // localmente tudo roda em HTTP e o navegador guardaria a regra para localhost.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            headers.XContentTypeOptions = "nosniff";              // não "adivinha" o tipo do conteúdo
            headers.XFrameOptions = "DENY";                       // não pode ser aberto em iframe (clickjacking)
            headers["Referrer-Policy"] = "no-referrer";           // não vaza a URL de origem
            headers.ContentSecurityPolicy = "frame-ancestors 'none'";

            // Respostas dos serviços passam pelo YARP com os headers deles: o que revela
            // a tecnologia sai no último momento, antes de a resposta ser enviada.
            // (O "Server" do próprio Gateway é desligado no Kestrel, em Program.cs.)
            context.Response.OnStarting(() =>
            {
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
                return Task.CompletedTask;
            });

            await next(context);
        });

        return app;
    }

    private static RateLimitPartition<string> FixedWindowPerIp(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
}

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int LoginPerMinute { get; init; } = 5;

    public int ApiPerMinute { get; init; } = 300;
}
