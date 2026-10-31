using BuildingBlocks.Application.Results;

namespace Catalog.Application.Products;

/// <summary>
/// Erros esperados do caso de uso de produtos, num lugar só (sem strings mágicas espalhadas).
/// </summary>
public static class ProductErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Product.NotFound", $"Product '{id}' was not found.");

    public static Error SkuAlreadyExists(string sku) =>
        Error.Conflict("Product.SkuAlreadyExists", $"A product with SKU '{sku}' already exists.");
}
