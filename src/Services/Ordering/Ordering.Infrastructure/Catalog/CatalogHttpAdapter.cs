using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Application.Results;
using Microsoft.Extensions.Logging;
using Ordering.Application.Abstractions;
using Polly;

namespace Ordering.Infrastructure.Catalog;

/// <summary>
/// Adapter / camada anticorrupção (ADR-16): implementa a porta IProductCatalog chamando a API
/// do Catálogo por HTTP e traduz o contrato externo para o modelo do contexto de Pedidos.
/// Se a API do Catálogo mudar, só este arquivo muda.
///
/// Typed HttpClient (ADR-05): o HttpClient vem do IHttpClientFactory, já com retry,
/// circuit breaker e timeout. Nunca "new HttpClient()" por requisição (esgota sockets).
/// </summary>
internal sealed class CatalogHttpAdapter(HttpClient httpClient, ILogger<CatalogHttpAdapter> logger)
    : IProductCatalog
{
    public async Task<Result<CatalogProduct>> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync($"api/products/{productId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<CatalogProduct>(ProductCatalogErrors.ProductNotFound(productId));
            }

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken)
                ?? throw new HttpRequestException("The catalog returned an empty body.");

            return Result.Success(new CatalogProduct(dto.Id, dto.Name, dto.Sku, dto.Price, dto.Currency, dto.StockQuantity));
        }
        catch (Exception exception) when (exception is HttpRequestException or ExecutionRejectedException)
        {
            // HttpRequestException: falha de rede ou status de erro depois das novas tentativas.
            // ExecutionRejectedException: circuito aberto ou timeout do Polly.
            logger.LogWarning(exception, "Catalog unavailable while fetching product {ProductId}", productId);
            return Result.Failure<CatalogProduct>(ProductCatalogErrors.Unavailable);
        }
    }

    /// <summary>
    /// Contrato da API do Catálogo, só com os campos que interessam aqui.
    /// Fica privado: nada fora do adaptador conhece o formato do outro serviço.
    /// </summary>
    private sealed record CatalogProductDto(
        Guid Id,
        string Name,
        string Sku,
        decimal Price,
        string Currency,
        int StockQuantity);
}
