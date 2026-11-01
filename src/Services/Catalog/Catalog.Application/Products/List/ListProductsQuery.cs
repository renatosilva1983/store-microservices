using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;
using FluentValidation;

namespace Catalog.Application.Products.List;

/// <summary>
/// Listagem com filtro por nome e categoria + paginação.
/// </summary>
public sealed record ListProductsQuery(string? Name, Guid? CategoryId, int Page = 1, int PageSize = 10)
    : IQuery<PagedResult<ProductResponse>>;

public sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public const int MaxPageSize = 100;

    public ListProductsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(query => query.Name).MaximumLength(100);
    }
}

public sealed class ListProductsQueryHandler(ICatalogQueries queries)
    : IQueryHandler<ListProductsQuery, PagedResult<ProductResponse>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(
        ListProductsQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new ProductFilter(query.Name, query.CategoryId, query.Page, query.PageSize);
        var page = await queries.ListProductsAsync(filter, cancellationToken);

        return Result.Success(page);
    }
}
