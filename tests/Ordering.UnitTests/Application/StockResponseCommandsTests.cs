using NSubstitute;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Stock;
using Ordering.Domain.Orders;

namespace Ordering.UnitTests.Application;

public class StockResponseCommandsTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private Order PendingOrder()
    {
        var order = Order.Create("Renato", "renato@example.com", DateTime.UtcNow);
        order.AddItem(Guid.NewGuid(), "Mouse", "MS-001", 100m, 1);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }

    [Fact]
    public async Task Confirm_PendingOrder_Confirms()
    {
        var order = PendingOrder();
        var handler = new ConfirmOrderStockCommandHandler(_orders, _unitOfWork);

        await handler.Handle(new ConfirmOrderStockCommand(order.Id), CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.Confirmed);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_SameMessageTwice_IsIdempotent()
    {
        var order = PendingOrder();
        var handler = new ConfirmOrderStockCommandHandler(_orders, _unitOfWork);

        await handler.Handle(new ConfirmOrderStockCommand(order.Id), CancellationToken.None);
        var second = await handler.Handle(new ConfirmOrderStockCommand(order.Id), CancellationToken.None);

        second.IsSuccess.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_PendingOrder_Cancels()
    {
        var order = PendingOrder();
        var handler = new RejectOrderStockCommandHandler(_orders, _unitOfWork, TimeProvider.System);

        await handler.Handle(new RejectOrderStockCommand(order.Id, "Sem estoque"), CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.Cancelled);
    }
}
