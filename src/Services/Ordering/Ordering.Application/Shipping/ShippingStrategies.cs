using Ordering.Domain.Orders;

namespace Ordering.Application.Shipping;

/// <summary>
/// Entrega padrão: valor fixo, grátis a partir de um valor mínimo de compra.
/// </summary>
public sealed class StandardShipping : IShippingStrategy
{
    public const decimal Fee = 20m;
    public const decimal FreeShippingThreshold = 300m;

    public ShippingMethod Method => ShippingMethod.Standard;

    public decimal Calculate(ShippingQuote quote) =>
        quote.Subtotal >= FreeShippingThreshold ? 0m : Fee;
}

/// <summary>
/// Entrega expressa: taxa base mais um adicional por unidade.
/// </summary>
public sealed class ExpressShipping : IShippingStrategy
{
    public const decimal BaseFee = 45m;
    public const decimal FeePerUnit = 2m;

    public ShippingMethod Method => ShippingMethod.Express;

    public decimal Calculate(ShippingQuote quote) => BaseFee + (FeePerUnit * quote.TotalQuantity);
}

/// <summary>
/// Retirada na loja: sem custo.
/// </summary>
public sealed class PickupInStoreShipping : IShippingStrategy
{
    public ShippingMethod Method => ShippingMethod.PickupInStore;

    public decimal Calculate(ShippingQuote quote) => 0m;
}
