using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Ordering.Application.Abstractions;

namespace Ordering.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IQuery<OrderResponse>;

public sealed class GetOrderByIdQueryHandler(IOrderQueries queries)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await queries.GetByIdAsync(query.Id, cancellationToken);

        return order is null
            ? Result.Failure<OrderResponse>(OrderErrors.NotFound(query.Id))
            : Result.Success(order);
    }
}

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

public sealed class ListOrdersQueryHandler(IOrderQueries queries)
    : IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    public async Task<Result<PagedResult<OrderSummaryResponse>>> Handle(
        ListOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var page = await queries.ListAsync(
            new OrderFilter(query.CustomerEmail, query.Page, query.PageSize),
            cancellationToken);

        return Result.Success(page);
    }
}
