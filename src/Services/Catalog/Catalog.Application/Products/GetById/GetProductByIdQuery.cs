using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;

namespace Catalog.Application.Products.GetById;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;

/// <summary>
/// Leitura: usa o lado de consulta (ICatalogQueries), não o repositório do domínio.
/// </summary>
public sealed class GetProductByIdQueryHandler(ICatalogQueries queries)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await queries.GetProductByIdAsync(query.Id, cancellationToken);

        return product is null
            ? Result.Failure<ProductResponse>(ProductErrors.NotFound(query.Id))
            : Result.Success(product);
    }
}
