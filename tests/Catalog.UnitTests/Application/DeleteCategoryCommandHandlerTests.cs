using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;
using Catalog.Application.Categories.Delete;
using Catalog.Domain.Categories;
using NSubstitute;

namespace Catalog.UnitTests.Application;

public class DeleteCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _handler = new DeleteCategoryCommandHandler(_categories, _products, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasProducts_ReturnsConflictAndKeepsCategory()
    {
        var category = Category.Create("Informática", null);
        _categories.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _products.AnyInCategoryAsync(category.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Category.HasProducts");
        _categories.DidNotReceive().Remove(Arg.Any<Category>());
    }

    [Fact]
    public async Task Handle_WhenCategoryIsEmpty_RemovesAndSaves()
    {
        var category = Category.Create("Livros", null);
        _categories.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _products.AnyInCategoryAsync(category.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _categories.Received(1).Remove(category);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _categories.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _handler.Handle(new DeleteCategoryCommand(id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
