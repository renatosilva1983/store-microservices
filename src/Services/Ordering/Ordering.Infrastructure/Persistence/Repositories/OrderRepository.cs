using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(OrderingDbContext context) : IOrderRepository
{
    // Owned entities (itens) são carregados automaticamente com o dono: o agregado vem inteiro.
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Orders.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public void Add(Order order) => context.Orders.Add(order);
}
