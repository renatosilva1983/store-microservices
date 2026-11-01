using Catalog.Domain.Common;

namespace Catalog.Domain.ValueObjects;

/// <summary>
/// Value Object (DDD): sem identidade, imutável, igual por valor.
/// Junta valor e moeda e garante as regras de dinheiro em um só lugar.
/// </summary>
public sealed record Money
{
    public const string DefaultCurrency = "BRL";
    public const int CurrencyLength = 3;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money.NegativeAmount", "Amount cannot be negative.");
        }

        var normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != CurrencyLength || !normalizedCurrency.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException("Money.InvalidCurrency", "Currency must be a 3-letter ISO code.");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), normalizedCurrency);
    }
}
