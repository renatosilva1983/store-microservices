using Ordering.Domain.Orders;

namespace Ordering.Application.Shipping;

/// <summary>
/// Escolhe a estratégia certa para o método pedido (Factory + Strategy).
/// </summary>
public interface IShippingCalculator
{
    decimal Calculate(ShippingMethod method, ShippingQuote quote);
}

/// <summary>
/// Recebe todas as estratégias registradas por injeção (IEnumerable) e indexa pelo método.
/// Por que não Keyed Services direto no handler: exigiria pedir a estratégia ao IServiceProvider
/// em tempo de execução (Service Locator). Aqui as dependências continuam explícitas.
/// </summary>
public sealed class ShippingCalculator : IShippingCalculator
{
    private readonly IReadOnlyDictionary<ShippingMethod, IShippingStrategy> _strategies;

    public ShippingCalculator(IEnumerable<IShippingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(strategy => strategy.Method);
    }

    public decimal Calculate(ShippingMethod method, ShippingQuote quote) =>
        _strategies.TryGetValue(method, out var strategy)
            ? strategy.Calculate(quote)
            : throw new InvalidOperationException($"No shipping strategy registered for '{method}'.");
}
