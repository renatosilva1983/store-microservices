using Catalog.Domain.Products;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Porta de saída (Ports and Adapters) — repositório por agregado (ADR-14).
/// A Application define; a Infrastructure implementa com EF Core.
/// Usado só pelo lado de escrita (commands).
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken);

    Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    void Add(Product product);

    void Remove(Product product);
}
