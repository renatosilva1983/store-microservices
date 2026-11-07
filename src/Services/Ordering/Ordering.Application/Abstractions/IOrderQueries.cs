using BuildingBlocks.Application.Pagination;
using Ordering.Application.Orders;

namespace Ordering.Application.Abstractions;

/// <summary>
/// Lado de leitura do CQRS para Pedidos.
/// <c>customerId</c> nulo = sem filtro de dono (administrador).
/// </summary>
public interface IOrderQueries
{
    Task<OrderResponse?> GetByIdAsync(Guid id, string? customerId, CancellationToken cancellationToken);

    Task<PagedResult<OrderSummaryResponse>> ListAsync(OrderFilter filter, CancellationToken cancellationToken);
}
