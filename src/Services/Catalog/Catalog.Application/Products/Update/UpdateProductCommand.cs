using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Products.Update;

/// <summary>
/// Edita dados e preço. O SKU é a identidade comercial do produto e não muda (decisão de negócio).
/// </summary>
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    Guid CategoryId) : ICommand;
