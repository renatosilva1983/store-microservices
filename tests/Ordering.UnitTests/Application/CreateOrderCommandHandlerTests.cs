using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Contracts.Orders;
using NSubstitute;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Create;
using Ordering.Application.Shipping;
using Ordering.Domain.Orders;

namespace Ordering.UnitTests.Application;

/// <summary>
/// O Catálogo é substituído por um mock da porta IProductCatalog:
/// o caso de uso é testado sem HTTP, sem banco e sem o outro serviço no ar.
/// </summary>
public class CreateOrderCommandHandlerTests
{
    private static readonly CatalogProduct Notebook =
        new(Guid.NewGuid(), "Notebook", "NB-001", 4000m, "BRL", StockQuantity: 5);

    private readonly IProductCatalog _catalog = Substitute.For<IProductCatalog>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventPublisher _events = Substitute.For<IIntegrationEventPublisher>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        var shipping = new ShippingCalculator([new StandardShipping(), new ExpressShipping(), new PickupInStoreShipping()]);
        _handler = new CreateOrderCommandHandler(_catalog, shipping, _orders, _unitOfWork, _events, _user, TimeProvider.System);

        // Usuário logado: o dono do pedido vem daqui, não do comando.
        _user.Id.Returns("renato@example.com");
        _user.Email.Returns("renato@example.com");
        _user.Name.Returns("Renato");
    }

    private static CreateOrderCommand Command(int quantity = 1, string shipping = "express") =>
        new(shipping, [new CreateOrderItem(Notebook.Id, quantity)]);

    [Fact]
    public async Task Handle_WithAvailableProduct_CreatesOrderWithCatalogPriceAndShipping()
    {
        _catalog.GetProductAsync(Notebook.Id, Arg.Any<CancellationToken>()).Returns(Result.Success(Notebook));

        var result = await _handler.Handle(Command(quantity: 2), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _orders.Received(1).Add(Arg.Is<Order>(order =>
            order.CustomerId == "renato@example.com" &&
            order.Subtotal == 8000m &&
            order.ShippingMethod == ShippingMethod.Express &&
            order.ShippingCost == 49m &&
            order.Total == 8049m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _events.Received(1).PublishAsync(
            Arg.Is<OrderCreatedIntegrationEvent>(e => e.Items.Single().Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameProductInTwoLines_ChecksStockForTheSum()
    {
        _catalog.GetProductAsync(Notebook.Id, Arg.Any<CancellationToken>()).Returns(Result.Success(Notebook));
        var command = new CreateOrderCommand(
            "standard",
            [new CreateOrderItem(Notebook.Id, 3), new CreateOrderItem(Notebook.Id, 3)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Error.Code.ShouldBe("Order.InsufficientStock");
        await _catalog.Received(1).GetProductAsync(Notebook.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ReturnsNotFound()
    {
        _catalog.GetProductAsync(Notebook.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CatalogProduct>(ProductCatalogErrors.ProductNotFound(Notebook.Id)));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _orders.DidNotReceive().Add(Arg.Any<Order>());
    }

    [Fact]
    public async Task Handle_WhenCatalogIsUnavailable_ReturnsUnavailableAndDoesNotSave()
    {
        _catalog.GetProductAsync(Notebook.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CatalogProduct>(ProductCatalogErrors.Unavailable));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _events.DidNotReceive().PublishAsync(Arg.Any<OrderCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutAuthenticatedUser_ReturnsFailureAndDoesNotSave()
    {
        _user.Id.Returns((string?)null);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.Code.ShouldBe("Order.Unauthenticated");
        _orders.DidNotReceive().Add(Arg.Any<Order>());
    }
}
