namespace Ordering.Api.Contracts;

public sealed record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string ShippingMethod,
    IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
