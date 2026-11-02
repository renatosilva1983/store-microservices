using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using Catalog.Application.Categories;
using Catalog.Application.Categories.Queries;
using Catalog.Application.Products;
using Catalog.Application.Products.GetById;
using Catalog.Application.Products.List;
using HotChocolate;
// HotChocolate também tem um tipo "Error": o alias deixa claro qual é o nosso.
using Error = BuildingBlocks.Application.Results.Error;

namespace Catalog.Api.GraphQL;

/// <summary>
/// Raiz das consultas GraphQL (ADR-32). Só leitura: escrita continua em REST.
/// Cada campo chama o MESMO query handler usado pelos controllers REST — GraphQL e REST
/// são duas bordas sobre a mesma Application (Clean Architecture na prática).
/// Os nomes viram campos sem o "Get": GetProducts → products.
/// </summary>
public sealed class CatalogQuery
{
    public async Task<ProductPage> GetProducts(
        [Service] IQueryHandler<ListProductsQuery, PagedResult<ProductResponse>> handler,
        string? name,
        Guid? categoryId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(new ListProductsQuery(name, categoryId, page, pageSize), cancellationToken);
        var value = result.IsSuccess ? result.Value : throw ToGraphQLException(result.Error);

        return new ProductPage(value.Items, value.Page, value.PageSize, value.TotalCount, value.TotalPages);
    }

    /// <summary>Em GraphQL, "não encontrado" é null no campo, não erro.</summary>
    public async Task<ProductResponse?> GetProduct(
        [Service] IQueryHandler<GetProductByIdQuery, ProductResponse> handler,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetProductByIdQuery(id), cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategories(
        [Service] IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ListCategoriesQuery(), cancellationToken);
        return result.IsSuccess ? result.Value : throw ToGraphQLException(result.Error);
    }

    /// <summary>Erro de negócio/validação vira um erro GraphQL com código (em "errors[].extensions.code").</summary>
    private static GraphQLException ToGraphQLException(Error error)
    {
        var message = error is ValidationError validation
            ? string.Join(" ", validation.Errors.SelectMany(field => field.Value.Select(m => $"{field.Key}: {m}")))
            : error.Description;

        return new GraphQLException(ErrorBuilder.New().SetMessage(message).SetCode(error.Code).Build());
    }
}

/// <summary>
/// Página de produtos no formato do GraphQL (tipo próprio em vez do PagedResult genérico,
/// para o schema ter um nome claro: ProductPage).
/// </summary>
public sealed record ProductPage(
    IReadOnlyList<ProductResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
