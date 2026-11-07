using BuildingBlocks.Application.Security;
using NSubstitute;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders;
using Ordering.Application.Orders.Cancel;
using Ordering.Application.Orders.Queries;
using Ordering.Domain.Orders;

namespace Ordering.UnitTests.Application;

/// <summary>
/// Proteção contra BOLA/IDOR (OWASP API1): o usuário A não lê nem cancela o pedido do usuário B,
/// mesmo sabendo o id. A resposta é "não encontrado", para não revelar que o pedido existe.
/// </summary>
public class OrderAccessTests
{
    private const string Owner = "cliente@store.dev";
    private const string OtherCustomer = "outro@store.dev";

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IOrderQueries _queries = Substitute.For<IOrderQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static ICurrentUser User(string id, string role = Roles.Customer)
    {
        var user = Substitute.For<ICurrentUser>();
        user.Id.Returns(id);
        user.IsInRole(role).Returns(true);
        return user;
    }

    private Order OwnersOrder()
    {
        var order = Order.Create(Owner, "Cliente", Owner, DateTime.UtcNow);
        order.AddItem(Guid.NewGuid(), "Mouse", "MS-001", 100m, 1);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }

    [Fact]
    public void OwnerFilter_ForCustomer_IsTheirOwnId()
    {
        OrderAccess.OwnerFilter(User(Owner)).ShouldBe(Owner);
    }

    [Fact]
    public void OwnerFilter_ForAdmin_IsNull()
    {
        OrderAccess.OwnerFilter(User("admin@store.dev", Roles.Admin)).ShouldBeNull();
    }

    [Fact]
    public async Task GetById_PassesTheOwnerFilterToTheQuery()
    {
        var id = Guid.NewGuid();
        var handler = new GetOrderByIdQueryHandler(_queries, User(OtherCustomer));

        var result = await handler.Handle(new GetOrderByIdQuery(id), CancellationToken.None);

        // A consulta recebe o dono; como o pedido não é dele, volta nulo → 404.
        await _queries.Received(1).GetByIdAsync(id, OtherCustomer, Arg.Any<CancellationToken>());
        result.Error.Code.ShouldBe("Order.NotFound");
    }

    [Fact]
    public async Task Cancel_OrderOfAnotherCustomer_ReturnsNotFoundAndDoesNotSave()
    {
        var order = OwnersOrder();
        var handler = new CancelOrderCommandHandler(_orders, _unitOfWork, User(OtherCustomer), TimeProvider.System);

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Error.Code.ShouldBe("Order.NotFound");
        order.Status.ShouldBe(OrderStatus.Pending);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_OwnOrder_Cancels()
    {
        var order = OwnersOrder();
        var handler = new CancelOrderCommandHandler(_orders, _unitOfWork, User(Owner), TimeProvider.System);

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_AdminCanCancelAnyOrder()
    {
        var order = OwnersOrder();
        var handler = new CancelOrderCommandHandler(_orders, _unitOfWork, User("admin@store.dev", Roles.Admin), TimeProvider.System);

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }
}
