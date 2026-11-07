using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Contracts.Orders;
using Ordering.Application.Abstractions;
using Ordering.Application.Shipping;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Create;

/// <summary>
/// Monta o pedido consultando o Catálogo (síncrono, ADR-05) e calcula o frete (Strategy, ADR-15).
/// A checagem de estoque aqui é só uma pré-validação para responder rápido ao cliente.
/// A baixa de verdade acontece no Catálogo, ao consumir o evento OrderCreated (ADR-06):
/// o pedido nasce Pending e vira Confirmed ou Cancelled quando o Catálogo responder.
/// </summary>
public sealed class CreateOrderCommandHandler(
    IProductCatalog catalog,
    IShippingCalculator shippingCalculator,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // O endpoint exige login; a checagem aqui protege o caso de uso de qualquer outra borda.
        if (currentUser.Id is null || currentUser.Email is null)
        {
            return Result.Failure<Guid>(OrderErrors.Unauthenticated);
        }

        var order = Order.Create(
            currentUser.Id,
            currentUser.Name ?? currentUser.Email,
            currentUser.Email,
            timeProvider.GetUtcNow().UtcDateTime);

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

        // Publicado ANTES do SaveChanges de propósito: com o Outbox, o evento é gravado
        // na mesma transação do pedido e só é entregue ao RabbitMQ depois do commit.
        await eventPublisher.PublishAsync(
            new OrderCreatedIntegrationEvent(
                order.Id,
                order.CreatedAtUtc,
                order.Items.Select(item => new OrderCreatedItem(item.ProductId, item.Sku, item.Quantity)).ToList()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
