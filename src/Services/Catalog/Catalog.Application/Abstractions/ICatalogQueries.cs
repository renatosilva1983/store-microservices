using BuildingBlocks.Application.Pagination;
using Catalog.Application.Categories;
using Catalog.Application.Products;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Lado de leitura do CQRS (ADR-12): consultas que vão direto para DTO,
/// sem carregar entidades nem passar pelo domínio. Implementado na Infrastructure
/// com projeções AsNoTracking (poderia ser Dapper sem mudar a Application).
/// </summary>
public interface ICatalogQueries
{
    Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ProductResponse>> ListProductsAsync(ProductFilter filter, CancellationToken cancellationToken);

    Task<CategoryResponse?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken);
}
