using Ordering.Domain.Orders;

namespace Ordering.Application.Abstractions;

/// <summary>
/// Porta de saída do agregado Order (lado de escrita).
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Order order);
}

/// <summary>
/// Unit of Work do serviço de Pedidos.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
