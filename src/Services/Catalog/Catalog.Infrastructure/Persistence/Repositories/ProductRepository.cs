using Catalog.Application.Abstractions;
using Catalog.Domain.Products;
using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de saída: implementa a porta IProductRepository com EF Core.
/// Não chama SaveChanges — quem confirma é o Unit of Work, no fim do caso de uso.
/// </summary>
internal sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.Sku == sku, cancellationToken);

    public Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.CategoryId == categoryId, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);

    public void Remove(Product product) => context.Products.Remove(product);
}
