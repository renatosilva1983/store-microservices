namespace Catalog.Application.Products;

/// <summary>
/// DTO de saída. A API nunca expõe a entidade (dados internos, acoplamento, mass assignment).
/// </summary>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAtUtc);

/// <summary>
/// Critérios da listagem paginada.
/// </summary>
public sealed record ProductFilter(string? Name, Guid? CategoryId, int Page, int PageSize);
