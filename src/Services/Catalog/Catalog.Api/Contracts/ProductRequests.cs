namespace Catalog.Api.Contracts;

/// <summary>
/// Contratos de entrada da API (o que o cliente envia). Ficam separados dos comandos:
/// a API pode evoluir (versionar, renomear campos) sem mexer nos casos de uso.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int InitialStock,
    Guid CategoryId);

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    Guid CategoryId);

public sealed record CategoryRequest(string Name, string? Description);
