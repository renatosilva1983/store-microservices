using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Contracts.Orders;
using Catalog.Application.Products.ReserveStock;
using MassTransit;

namespace Catalog.Infrastructure.Messaging;

/// <summary>
/// Adaptador de entrada: recebe o evento do Ordering e chama o caso de uso.
/// Idempotência: o Inbox do MassTransit (UseEntityFrameworkOutbox) registra o MessageId;
/// se o RabbitMQ entregar a mesma mensagem de novo, ela é descartada e o estoque não baixa duas vezes.
/// </summary>
internal sealed class OrderCreatedConsumer(ICommandHandler<ReserveStockForOrderCommand> handler)
    : IConsumer<OrderCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var command = new ReserveStockForOrderCommand(
            context.Message.OrderId,
            context.Message.Items.Select(item => new ReserveStockItem(item.ProductId, item.Quantity)).ToList());

        await handler.Handle(command, context.CancellationToken);
    }
}
