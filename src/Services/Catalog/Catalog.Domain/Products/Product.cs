using Catalog.Domain.Common;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Products;

/// <summary>
/// Produto — raiz de agregado (DDD tático, ADR-11).
/// Modelo rico: o estado só muda pelos métodos, que protegem as regras.
/// A categoria é referenciada só pelo Id: um agregado não carrega outro agregado dentro.
/// </summary>
public sealed class Product : Entity
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 1000;

    private Product(Guid id, Sku sku, DateTime createdAtUtc)
        : base(id)
    {
        Sku = sku;
        CreatedAtUtc = createdAtUtc;
        Name = string.Empty;
        Price = Money.Create(0);
    }

    // EF Core
    private Product()
    {
        Name = string.Empty;
        Sku = null!;
        Price = null!;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Sku Sku { get; private set; }

    public Money Price { get; private set; }

    public int StockQuantity { get; private set; }

    public Guid CategoryId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Product Create(
        string name,
        string? description,
        Sku sku,
        Money price,
        int initialStock,
        Guid categoryId,
        DateTime createdAtUtc)
    {
        if (initialStock < 0)
        {
            throw new DomainException("Product.NegativeStock", "Initial stock cannot be negative.");
        }

        var product = new Product(Guid.NewGuid(), sku, createdAtUtc)
        {
            StockQuantity = initialStock
        };

        product.UpdateDetails(name, description, categoryId);
        product.ChangePrice(price);
        return product;
    }

    public void UpdateDetails(string name, string? description, Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Product.CategoryRequired", "Category is required.");
        }

        Name = Guard.RequiredText(name, NameMaxLength, "Product.Name");
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Product.Description");
        CategoryId = categoryId;
    }

    public void ChangePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        Price = newPrice;
    }

    public void AddStock(int quantity)
    {
        EnsurePositive(quantity);
        StockQuantity += quantity;
    }

    public void DebitStock(int quantity)
    {
        EnsurePositive(quantity);

        if (quantity > StockQuantity)
        {
            throw new DomainException(
                "Product.InsufficientStock",
                $"Insufficient stock: requested {quantity}, available {StockQuantity}.");
        }

        StockQuantity -= quantity;
    }

    private static void EnsurePositive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Product.InvalidQuantity", "Quantity must be greater than zero.");
        }
    }
}
