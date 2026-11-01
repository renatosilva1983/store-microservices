namespace Ordering.Infrastructure.Catalog;

/// <summary>
/// Options Pattern (ADR-19): configuração tipada, validada na inicialização.
/// Local: http://localhost:5086 | Docker: http://catalog-api:8080 (variável de ambiente).
/// </summary>
public sealed class CatalogServiceOptions
{
    public const string SectionName = "Services:Catalog";

    public string BaseUrl { get; init; } = string.Empty;
}
