using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Application.Security;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Cancel;

public sealed record CancelOrderCommand(Guid Id) : ICommand;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.Id, cancellationToken);
        // Pedido de outro usuário responde igual a pedido inexistente (404): não revela que ele existe.
        if (order is null || !OrderAccess.CanAccess(order, currentUser))
        {
            return Result.Failure(OrderErrors.NotFound(command.Id));
        }

        // Erro esperado → Result (409). O domínio também protege, mas aí seria uma exceção.
        if (order.Status == OrderStatus.Cancelled)
        {
            return Result.Failure(OrderErrors.AlreadyCancelled(command.Id));
        }

        order.Cancel(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
