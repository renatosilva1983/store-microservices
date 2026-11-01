using Ordering.Domain.Common;

namespace Ordering.Domain.Orders;

/// <summary>
/// Item do pedido. Faz parte do agregado Order: só é criado e alterado pela raiz.
/// Guarda uma cópia do nome, SKU e preço no momento da compra — se o Catálogo mudar
/// o preço depois, o pedido não muda. É o "produto" na linguagem do contexto de Pedidos.
/// </summary>
public sealed class OrderItem : Entity
{
    internal OrderItem(Guid productId, string productName, string sku, decimal unitPrice, int quantity)
        : base(Guid.NewGuid())
    {
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    // EF Core
    private OrderItem()
    {
        ProductName = string.Empty;
        Sku = string.Empty;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public string Sku { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal void IncreaseQuantity(int quantity) => Quantity += quantity;
}
