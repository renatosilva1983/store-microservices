using Ordering.Domain.Common;

namespace Ordering.Domain.Orders;

/// <summary>
/// Pedido — raiz do agregado (DDD tático, ADR-11).
/// Itens só entram e mudam por aqui; a raiz garante as invariantes:
/// - só se altera pedido pendente;
/// - pedido sem itens não pode ser confirmado;
/// - totais sempre coerentes com os itens e o frete.
/// </summary>
public sealed class Order : Entity
{
    public const string DefaultCurrency = "BRL";
    public const int CustomerNameMaxLength = 150;
    public const int CustomerEmailMaxLength = 200;
    public const int MaxQuantityPerItem = 100;

    private readonly List<OrderItem> _items = [];

    private Order(Guid id, string customerName, string customerEmail, DateTime createdAtUtc)
        : base(id)
    {
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CreatedAtUtc = createdAtUtc;
        Currency = DefaultCurrency;
        Status = OrderStatus.Pending;
        ShippingMethod = ShippingMethod.Standard;
    }

    // EF Core
    private Order()
    {
        CustomerName = string.Empty;
        CustomerEmail = string.Empty;
        Currency = DefaultCurrency;
    }

    public string CustomerName { get; private set; }

    public string CustomerEmail { get; private set; }

    public OrderStatus Status { get; private set; }

    public ShippingMethod ShippingMethod { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal ShippingCost { get; private set; }

    public decimal Total { get; private set; }

    public string Currency { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public int TotalQuantity => _items.Sum(item => item.Quantity);

    public static Order Create(string customerName, string customerEmail, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(customerName) || customerName.Trim().Length > CustomerNameMaxLength)
        {
            throw new DomainException("Order.InvalidCustomerName", "Customer name is required and must be short.");
        }

        if (string.IsNullOrWhiteSpace(customerEmail) || customerEmail.Trim().Length > CustomerEmailMaxLength)
        {
            throw new DomainException("Order.InvalidCustomerEmail", "Customer e-mail is required and must be short.");
        }

        return new Order(Guid.NewGuid(), customerName.Trim(), customerEmail.Trim().ToLowerInvariant(), createdAtUtc);
    }

    public void AddItem(Guid productId, string productName, string sku, decimal unitPrice, int quantity)
    {
        EnsurePending();

        if (productId == Guid.Empty)
        {
            throw new DomainException("Order.InvalidProduct", "Product is required.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Order.NegativePrice", "Unit price cannot be negative.");
        }

        var existing = _items.SingleOrDefault(item => item.ProductId == productId);
        var newQuantity = (existing?.Quantity ?? 0) + quantity;
        if (quantity <= 0 || newQuantity > MaxQuantityPerItem)
        {
            throw new DomainException(
                "Order.InvalidQuantity",
                $"Quantity must be between 1 and {MaxQuantityPerItem} per product.");
        }

        if (existing is null)
        {
            _items.Add(new OrderItem(productId, productName, sku, unitPrice, quantity));
        }
        else
        {
            existing.IncreaseQuantity(quantity);
        }

        RecalculateTotals();
    }

    public void SetShipping(ShippingMethod method, decimal cost)
    {
        EnsurePending();

        if (cost < 0)
        {
            throw new DomainException("Order.NegativeShipping", "Shipping cost cannot be negative.");
        }

        ShippingMethod = method;
        ShippingCost = cost;
        RecalculateTotals();
    }

    public void Confirm()
    {
        EnsurePending();

        if (_items.Count == 0)
        {
            throw new DomainException("Order.Empty", "An order without items cannot be confirmed.");
        }

        Status = OrderStatus.Confirmed;
    }

    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("Order.AlreadyCancelled", "The order is already cancelled.");
        }

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Order.NotPending", $"Only pending orders can be changed (current: {Status}).");
        }
    }

    private void RecalculateTotals()
    {
        Subtotal = _items.Sum(item => item.LineTotal);
        Total = Subtotal + ShippingCost;
    }
}
