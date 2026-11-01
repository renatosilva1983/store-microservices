using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Stock;

/// <summary>O Catálogo reservou o estoque: o pedido é confirmado.</summary>
public sealed record ConfirmOrderStockCommand(Guid OrderId) : ICommand;

/// <summary>O Catálogo recusou o estoque: o pedido é cancelado.</summary>
public sealed record RejectOrderStockCommand(Guid OrderId, string Reason) : ICommand;

/// <summary>
/// Idempotente: se o pedido já saiu de Pending (mensagem repetida ou cancelado pelo cliente),
/// não faz nada e devolve sucesso — processar duas vezes dá o mesmo resultado que uma.
/// </summary>
public sealed class ConfirmOrderStockCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmOrderStockCommand>
{
    public async Task<Result> Handle(ConfirmOrderStockCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound(command.OrderId));
        }

        if (order.Status != OrderStatus.Pending)
        {
            return Result.Success();
        }

        order.Confirm();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RejectOrderStockCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RejectOrderStockCommand>
{
    public async Task<Result> Handle(RejectOrderStockCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound(command.OrderId));
        }

        if (order.Status != OrderStatus.Pending)
        {
            return Result.Success();
        }

        order.Cancel(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
