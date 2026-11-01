using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Contracts.Catalog;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.Orders.Stock;

namespace Ordering.Infrastructure.Messaging;

/// <summary>
/// Consumidores são adaptadores de ENTRADA (como os controllers): recebem a mensagem
/// e chamam o caso de uso. Nenhuma regra de negócio aqui.
/// </summary>
internal sealed class StockReservedConsumer(
    ICommandHandler<ConfirmOrderStockCommand> handler,
    ILogger<StockReservedConsumer> logger)
    : IConsumer<StockReservedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<StockReservedIntegrationEvent> context)
    {
        var result = await handler.Handle(new ConfirmOrderStockCommand(context.Message.OrderId), context.CancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("Could not confirm order {OrderId}: {Error}", context.Message.OrderId, result.Error.Code);
        }
    }
}

internal sealed class StockRejectedConsumer(
    ICommandHandler<RejectOrderStockCommand> handler,
    ILogger<StockRejectedConsumer> logger)
    : IConsumer<StockRejectedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<StockRejectedIntegrationEvent> context)
    {
        logger.LogInformation("Stock rejected for order {OrderId}: {Reason}", context.Message.OrderId, context.Message.Reason);

        var result = await handler.Handle(
            new RejectOrderStockCommand(context.Message.OrderId, context.Message.Reason),
            context.CancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("Could not cancel order {OrderId}: {Error}", context.Message.OrderId, result.Error.Code);
        }
    }
}
