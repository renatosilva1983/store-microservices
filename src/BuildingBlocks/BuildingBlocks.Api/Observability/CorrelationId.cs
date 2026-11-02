using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Api.Observability;

/// <summary>
/// Correlation Id (ADR-08): um identificador por requisição que atravessa os serviços.
/// Chega no header X-Correlation-Id (ou é criado aqui), vai para o escopo de todos os logs
/// e volta na resposta. Com ele dá para seguir um pedido do Gateway até o Catálogo.
/// </summary>
public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    internal const string ItemKey = "CorrelationId";

    public static string? Current(HttpContext? context) =>
        context?.Items.TryGetValue(ItemKey, out var value) == true ? value as string : null;
}

internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationId.HeaderName, out var values)
                            && !string.IsNullOrWhiteSpace(values.ToString())
            ? values.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[CorrelationId.ItemKey] = correlationId;
        context.Response.Headers[CorrelationId.HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}

/// <summary>
/// Propaga o Correlation Id nas chamadas HTTP que este serviço faz para outros (ex.: Ordering → Catalog).
/// </summary>
internal sealed class CorrelationIdDelegatingHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationId.Current(accessor.HttpContext);
        if (correlationId is not null && !request.Headers.Contains(CorrelationId.HeaderName))
        {
            request.Headers.Add(CorrelationId.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
