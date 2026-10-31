using System.Text.RegularExpressions;
using Catalog.Domain.Common;

namespace Catalog.Domain.ValueObjects;

/// <summary>
/// Value Object do código do produto (Stock Keeping Unit).
/// Normaliza (maiúsculas, sem espaços) e garante o formato.
/// </summary>
public sealed partial record Sku
{
    public const int MinLength = 3;
    public const int MaxLength = 20;

    private Sku(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Sku Create(string value)
    {
        var normalized = Normalize(value);
        if (!IsValid(normalized))
        {
            throw new DomainException(
                "Sku.InvalidFormat",
                $"SKU must have {MinLength} to {MaxLength} characters: letters, digits or hyphen.");
        }

        return new Sku(normalized);
    }

    /// <summary>
    /// Usado também pelo validador da Application: a regra de formato vive num lugar só.
    /// </summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && SkuPattern().IsMatch(Normalize(value));

    public override string ToString() => Value;

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    [GeneratedRegex("^[A-Z0-9-]{3,20}$")]
    private static partial Regex SkuPattern();
}
