using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;

namespace Catalog.Application.Categories.Queries;

public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryResponse>;

public sealed class GetCategoryByIdQueryHandler(ICatalogQueries queries)
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        var category = await queries.GetCategoryByIdAsync(query.Id, cancellationToken);

        return category is null
            ? Result.Failure<CategoryResponse>(CategoryErrors.NotFound(query.Id))
            : Result.Success(category);
    }
}

public sealed record ListCategoriesQuery : IQuery<IReadOnlyList<CategoryResponse>>;

public sealed class ListCategoriesQueryHandler(ICatalogQueries queries)
    : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(
        ListCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        var categories = await queries.ListCategoriesAsync(cancellationToken);
        return Result.Success(categories);
    }
}
