using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence;

/// <summary>
/// Banco próprio do serviço de Pedidos (Database per Service, ADR-03).
/// Nenhuma tabela do Catálogo existe aqui: o que o pedido precisa do produto foi copiado no item.
/// </summary>
public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "ordering";

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
