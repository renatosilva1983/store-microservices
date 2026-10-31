using BuildingBlocks.Application.Pagination;
using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Catalog.Application.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de leitura do CQRS: AsNoTracking + projeção direto para o formato de saída.
/// Detalhes do provedor (ILike do PostgreSQL) ficam aqui, na Infrastructure.
/// </summary>
internal sealed class CatalogQueries(CatalogDbContext context) : ICatalogQueries
{
    public async Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var rows = await ProductRows()
            .Where(row => row.Id == id)
            .ToListAsync(cancellationToken);

        return rows.Select(ToResponse).SingleOrDefault();
    }

    public async Task<PagedResult<ProductResponse>> ListProductsAsync(
        ProductFilter filter,
        CancellationToken cancellationToken)
    {
        var query = ProductRows();

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var pattern = $"%{filter.Name.Trim()}%";
            query = query.Where(row => EF.Functions.ILike(row.Name, pattern));
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(row => row.CategoryId == categoryId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(row => row.Name)
            .ThenBy(row => row.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>(
            rows.Select(ToResponse).ToList(),
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    public Task<CategoryResponse?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken) =>
        await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken);

    // Junta produto + nome da categoria numa consulta só (read model).
    private IQueryable<ProductRow> ProductRows() =>
        from product in context.Products.AsNoTracking()
        join category in context.Categories.AsNoTracking() on product.CategoryId equals category.Id
        select new ProductRow
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            CreatedAtUtc = product.CreatedAtUtc
        };

    private static ProductResponse ToResponse(ProductRow row) =>
        new(
            row.Id,
            row.Name,
            row.Description,
            row.Sku.Value,
            row.Price,
            row.Currency,
            row.StockQuantity,
            row.CategoryId,
            row.CategoryName,
            row.CreatedAtUtc);

    private sealed class ProductRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Domain.ValueObjects.Sku Sku { get; init; } = null!;
        public decimal Price { get; init; }
        public string Currency { get; init; } = string.Empty;
        public int StockQuantity { get; init; }
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
    }
}
