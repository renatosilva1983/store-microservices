using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Ordering.Application.Abstractions;
using Ordering.Application.Shipping;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Create;

/// <summary>
/// Monta o pedido consultando o Catálogo (síncrono, ADR-05) e calcula o frete (Strategy, ADR-15).
/// A baixa de estoque de verdade acontece no Catálogo, via evento, na Fase 3.
/// Aqui a checagem de estoque é só uma pré-validação para dar resposta rápida ao cliente.
/// </summary>
public sealed class CreateOrderCommandHandler(
    IProductCatalog catalog,
    IShippingCalculator shippingCalculator,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = Order.Create(command.CustomerName, command.CustomerEmail, timeProvider.GetUtcNow().UtcDateTime);

        // Mesmo produto em linhas diferentes vira uma linha só, com a quantidade somada.
        var requestedItems = command.Items
            .GroupBy(item => item.ProductId)
            .Select(group => (ProductId: group.Key, Quantity: group.Sum(item => item.Quantity)));

        foreach (var (productId, quantity) in requestedItems)
        {
            var productResult = await catalog.GetProductAsync(productId, cancellationToken);
            if (productResult.IsFailure)
            {
                return Result.Failure<Guid>(productResult.Error);
            }

            var product = productResult.Value;

            if (product.Currency != order.Currency)
            {
                return Result.Failure<Guid>(OrderErrors.UnsupportedCurrency(product.Sku, product.Currency));
            }

            if (product.StockQuantity < quantity)
            {
                return Result.Failure<Guid>(OrderErrors.InsufficientStock(product.Sku, quantity, product.StockQuantity));
            }

            order.AddItem(product.Id, product.Name, product.Sku, product.Price, quantity);
        }

        var method = Enum.Parse<ShippingMethod>(command.ShippingMethod, ignoreCase: true);
        var shippingCost = shippingCalculator.Calculate(method, new ShippingQuote(order.Subtotal, order.TotalQuantity));
        order.SetShipping(method, shippingCost);

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
