namespace Catalog.Domain.Common;

/// <summary>
/// Verificações repetidas das invariantes de texto (evita duplicação — Clean Code / DRY).
/// </summary>
internal static class Guard
{
    public static string RequiredText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{field}.Required", $"{field} is required.");
        }

        return MaxLength(value.Trim(), maxLength, field);
    }

    public static string? OptionalText(string? value, int maxLength, string field) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(value.Trim(), maxLength, field);

    private static string MaxLength(string value, int maxLength, string field)
    {
        if (value.Length > maxLength)
        {
            throw new DomainException($"{field}.TooLong", $"{field} must have at most {maxLength} characters.");
        }

        return value;
    }
}
