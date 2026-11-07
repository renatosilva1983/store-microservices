using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Application.Security;
using FluentValidation;
using Ordering.Application.Abstractions;

namespace Ordering.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IQuery<OrderResponse>;

public sealed class GetOrderByIdQueryHandler(IOrderQueries queries, ICurrentUser currentUser)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        // O filtro de dono vai para a própria consulta: pedido de outro usuário volta como "não encontrado".
        var order = await queries.GetByIdAsync(query.Id, OrderAccess.OwnerFilter(currentUser), cancellationToken);

        return order is null
            ? Result.Failure<OrderResponse>(OrderErrors.NotFound(query.Id))
            : Result.Success(order);
    }
}

/// <param name="CustomerEmail">Filtro por e-mail, só para o administrador. O cliente vê sempre só os próprios pedidos.</param>
public sealed record ListOrdersQuery(string? CustomerEmail, int Page = 1, int PageSize = 10)
    : IQuery<PagedResult<OrderSummaryResponse>>;

public sealed class ListOrdersQueryValidator : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ListOrdersQueryHandler(IOrderQueries queries, ICurrentUser currentUser)
    : IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    public async Task<Result<PagedResult<OrderSummaryResponse>>> Handle(
        ListOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var page = await queries.ListAsync(
            new OrderFilter(OrderAccess.OwnerFilter(currentUser), query.CustomerEmail, query.Page, query.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}
