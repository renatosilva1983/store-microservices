using BuildingBlocks.Application.Results;

namespace Catalog.Application.Categories;

public static class CategoryErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Category.NotFound", $"Category '{id}' was not found.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("Category.NameAlreadyExists", $"A category named '{name}' already exists.");

    public static Error HasProducts(Guid id) =>
        Error.Conflict("Category.HasProducts", $"Category '{id}' has products and cannot be deleted.");
}
