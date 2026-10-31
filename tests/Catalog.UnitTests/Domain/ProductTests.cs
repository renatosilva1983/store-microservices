using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Catalog.Domain.ValueObjects;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Product CreateProduct(int initialStock = 10) =>
        Product.Create(
            "Notebook",
            "14 polegadas",
            Sku.Create("NB-001"),
            Money.Create(4500m),
            initialStock,
            CategoryId,
            new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var product = CreateProduct();

        product.Id.ShouldNotBe(Guid.Empty);
        product.Name.ShouldBe("Notebook");
        product.Sku.Value.ShouldBe("NB-001");
        product.Price.Amount.ShouldBe(4500m);
        product.StockQuantity.ShouldBe(10);
        product.CategoryId.ShouldBe(CategoryId);
    }

    [Fact]
    public void Create_WithBlankName_ThrowsDomainException()
    {
        var exception = Should.Throw<DomainException>(() =>
            Product.Create(" ", null, Sku.Create("NB-001"), Money.Create(1m), 0, CategoryId, DateTime.UtcNow));

        exception.Code.ShouldBe("Product.Name.Required");
    }

    [Fact]
    public void Create_WithNegativeStock_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => CreateProduct(initialStock: -1))
            .Code.ShouldBe("Product.NegativeStock");
    }

    [Fact]
    public void DebitStock_WithAvailableQuantity_ReducesStock()
    {
        var product = CreateProduct(initialStock: 10);

        product.DebitStock(3);

        product.StockQuantity.ShouldBe(7);
    }

    [Fact]
    public void DebitStock_MoreThanAvailable_ThrowsAndKeepsStock()
    {
        var product = CreateProduct(initialStock: 2);

        Should.Throw<DomainException>(() => product.DebitStock(3))
            .Code.ShouldBe("Product.InsufficientStock");

        product.StockQuantity.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddStock_WithNonPositiveQuantity_Throws(int quantity)
    {
        var product = CreateProduct();

        Should.Throw<DomainException>(() => product.AddStock(quantity))
            .Code.ShouldBe("Product.InvalidQuantity");
    }

    [Fact]
    public void ChangePrice_ReplacesPrice()
    {
        var product = CreateProduct();

        product.ChangePrice(Money.Create(3999.90m));

        product.Price.ShouldBe(Money.Create(3999.90m));
    }
}
