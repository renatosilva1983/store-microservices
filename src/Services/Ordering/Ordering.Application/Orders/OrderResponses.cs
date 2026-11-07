namespace Ordering.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    string Status,
    string ShippingMethod,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string Currency,
    DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderSummaryResponse(
    Guid Id,
    string CustomerName,
    string Status,
    decimal Total,
    string Currency,
    int ItemCount,
    DateTime CreatedAtUtc);

public sealed record OrderFilter(string? CustomerId, string? CustomerEmail, int Page, int PageSize);
