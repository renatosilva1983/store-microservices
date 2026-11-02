using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Contracts.Catalog;
using Catalog.Application.Abstractions;
using Catalog.Application.Products.ReserveStock;
using Catalog.Domain.Products;
using Catalog.Domain.ValueObjects;
using NSubstitute;

namespace Catalog.UnitTests.Application;

public class ReserveStockForOrderCommandHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIntegrationEventPublisher _events = Substitute.For<IIntegrationEventPublisher>();
    private readonly ReserveStockForOrderCommandHandler _handler;

    public ReserveStockForOrderCommandHandlerTests()
    {
        _handler = new ReserveStockForOrderCommandHandler(_products, _unitOfWork, _events);
    }

    private Product NewProduct(string sku, int stock)
    {
        var product = Product.Create(
            sku, null, Sku.Create(sku), Money.Create(10m), stock, Guid.NewGuid(), DateTime.UtcNow);
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        return product;
    }

    [Fact]
    public async Task Handle_AllItemsAvailable_DebitsAndPublishesReserved()
    {
        var notebook = NewProduct("NB-001", 5);
        var mouse = NewProduct("MS-001", 10);
        var orderId = Guid.NewGuid();

        await _handler.Handle(
            new ReserveStockForOrderCommand(orderId, [new(notebook.Id, 2), new(mouse.Id, 3)]),
            CancellationToken.None);

        notebook.StockQuantity.ShouldBe(3);
        mouse.StockQuantity.ShouldBe(7);
        await _events.Received(1).PublishAsync(
            Arg.Is<StockReservedIntegrationEvent>(e => e.OrderId == orderId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OneItemWithoutStock_DebitsNothingAndPublishesRejected()
    {
        var notebook = NewProduct("NB-001", 5);
        var mouse = NewProduct("MS-001", 1);

        await _handler.Handle(
            new ReserveStockForOrderCommand(Guid.NewGuid(), [new(notebook.Id, 2), new(mouse.Id, 3)]),
            CancellationToken.None);

        notebook.StockQuantity.ShouldBe(5);
        mouse.StockQuantity.ShouldBe(1);
        await _events.Received(1).PublishAsync(Arg.Any<StockRejectedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _events.DidNotReceive().PublishAsync(Arg.Any<StockReservedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
