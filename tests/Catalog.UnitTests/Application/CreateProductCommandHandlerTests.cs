using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;
using Catalog.Application.Products.Create;
using Catalog.Domain.Products;
using Catalog.Domain.ValueObjects;
using NSubstitute;

namespace Catalog.UnitTests.Application;

/// <summary>
/// Testa o caso de uso sem banco: as portas são substituídas por mocks (NSubstitute).
/// É a prova prática do ganho da inversão de dependência.
/// </summary>
public class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductCommandHandler _handler;

    private static readonly CreateProductCommand ValidCommand =
        new("Notebook", "14 polegadas", "nb-001", 4500m, 10, Guid.NewGuid());

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(_products, _categories, _unitOfWork, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsProductAndSaves()
    {
        _categories.ExistsAsync(ValidCommand.CategoryId, Arg.Any<CancellationToken>()).Returns(true);
        _products.SkuExistsAsync(Arg.Any<Sku>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        _products.Received(1).Add(Arg.Is<Product>(p => p.Sku.Value == "NB-001" && p.Id == result.Value));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ReturnsNotFoundAndDoesNotSave()
    {
        _categories.ExistsAsync(ValidCommand.CategoryId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Category.NotFound");
        _products.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSkuAlreadyExists_ReturnsConflict()
    {
        _categories.ExistsAsync(ValidCommand.CategoryId, Arg.Any<CancellationToken>()).Returns(true);
        _products.SkuExistsAsync(Arg.Any<Sku>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Product.SkuAlreadyExists");
        _products.DidNotReceive().Add(Arg.Any<Product>());
    }
}
