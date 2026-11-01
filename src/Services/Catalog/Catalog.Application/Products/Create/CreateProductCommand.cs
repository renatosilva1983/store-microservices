using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Products.Create;

/// <summary>
/// Comando de escrita: devolve o Id do produto criado.
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int InitialStock,
    Guid CategoryId) : ICommand<Guid>;
