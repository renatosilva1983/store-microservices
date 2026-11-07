using BuildingBlocks.Application.Results;

namespace Ordering.Application.Orders;

public static class OrderErrors
{
    public static readonly Error Unauthenticated =
        Error.Failure("Order.Unauthenticated", "An authenticated customer is required to place an order.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Order.NotFound", $"Order '{id}' was not found.");

    public static Error InsufficientStock(string sku, int requested, int available) =>
        Error.Conflict(
            "Order.InsufficientStock",
            $"Insufficient stock for '{sku}': requested {requested}, available {available}.");

    public static Error UnsupportedCurrency(string sku, string currency) =>
        Error.Conflict("Order.UnsupportedCurrency", $"Product '{sku}' is priced in '{currency}', which is not supported.");

    public static Error AlreadyCancelled(Guid id) =>
        Error.Conflict("Order.AlreadyCancelled", $"Order '{id}' is already cancelled.");
}
