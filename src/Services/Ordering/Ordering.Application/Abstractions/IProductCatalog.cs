using BuildingBlocks.Application.Results;

namespace Ordering.Application.Abstractions;

/// <summary>
/// Porta para o outro serviço (Catálogo), na linguagem do contexto de Pedidos.
/// A Application não sabe que existe HTTP, JSON ou retry: isso é do adaptador
/// (CatalogHttpAdapter, na Infrastructure) — Adapter / camada anticorrupção (ADR-16).
/// </summary>
public interface IProductCatalog
{
    Task<Result<CatalogProduct>> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}

/// <summary>
/// O que o Pedido precisa saber de um produto. Não é o Product do Catálogo:
/// cada contexto tem o seu modelo.
/// </summary>
public sealed record CatalogProduct(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity);

public static class ProductCatalogErrors
{
    public static Error ProductNotFound(Guid productId) =>
        Error.NotFound("Catalog.ProductNotFound", $"Product '{productId}' was not found in the catalog.");

    public static readonly Error Unavailable =
        Error.Unavailable("Catalog.Unavailable", "The catalog service is unavailable. Please try again later.");
}
