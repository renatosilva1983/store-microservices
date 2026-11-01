using Ordering.Domain.Orders;

namespace Ordering.Application.Shipping;

/// <summary>
/// Strategy (ADR-15): cada forma de entrega é um algoritmo de cálculo intercambiável.
/// Novo tipo de frete = nova classe; nenhuma existente muda (Open/Closed).
/// </summary>
public interface IShippingStrategy
{
    ShippingMethod Method { get; }

    decimal Calculate(ShippingQuote quote);
}

/// <summary>
/// Dados de que o cálculo precisa, sem expor o pedido inteiro à estratégia.
/// </summary>
public sealed record ShippingQuote(decimal Subtotal, int TotalQuantity);
