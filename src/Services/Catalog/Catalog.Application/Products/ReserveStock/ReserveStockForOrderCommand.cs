using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Contracts.Catalog;
using Catalog.Application.Abstractions;
using Catalog.Domain.Products;

namespace Catalog.Application.Products.ReserveStock;

public sealed record ReserveStockForOrderCommand(Guid OrderId, IReadOnlyList<ReserveStockItem> Items) : ICommand;

public sealed record ReserveStockItem(Guid ProductId, int Quantity);

/// <summary>
/// Baixa o estoque de um pedido: tudo ou nada. Primeiro valida todos os itens; só se todos
/// tiverem saldo é que debita. Responde ao Ordering com um evento (coreografia, sem orquestrador).
/// Falta de estoque é resultado de negócio (evento StockRejected), não erro técnico.
/// </summary>
public sealed class ReserveStockForOrderCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<ReserveStockForOrderCommand>
{
    public async Task<Result> Handle(ReserveStockForOrderCommand command, CancellationToken cancellationToken)
    {
        var reservations = new List<(Product Product, int Quantity)>();

        foreach (var item in command.Items)
        {
            var product = await products.GetByIdAsync(item.ProductId, cancellationToken);
            var rejection = product switch
            {
                null => $"Product '{item.ProductId}' not found.",
                _ when product.StockQuantity < item.Quantity =>
                    $"Insufficient stock for '{product.Sku}': requested {item.Quantity}, available {product.StockQuantity}.",
                _ => null
            };

            if (rejection is not null)
            {
                await eventPublisher.PublishAsync(new StockRejectedIntegrationEvent(command.OrderId, rejection), cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            reservations.Add((product!, item.Quantity));
        }

        foreach (var (product, quantity) in reservations)
        {
            product.DebitStock(quantity);
        }

        await eventPublisher.PublishAsync(new StockReservedIntegrationEvent(command.OrderId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
