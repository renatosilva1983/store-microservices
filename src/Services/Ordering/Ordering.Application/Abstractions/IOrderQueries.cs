using BuildingBlocks.Application.Pagination;
using Ordering.Application.Orders;

namespace Ordering.Application.Abstractions;

/// <summary>
/// Lado de leitura do CQRS para Pedidos.
/// </summary>
public interface IOrderQueries
{
    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<OrderSummaryResponse>> ListAsync(OrderFilter filter, CancellationToken cancellationToken);
}
