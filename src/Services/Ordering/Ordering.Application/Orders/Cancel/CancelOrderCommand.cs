using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Ordering.Application.Abstractions;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Cancel;

public sealed record CancelOrderCommand(Guid Id) : ICommand;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.Id, cancellationToken);
        if (order is null)
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
