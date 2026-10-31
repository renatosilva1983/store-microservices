using Catalog.Domain.Common;

namespace Catalog.Domain.Categories;

/// <summary>
/// Categoria de produtos. Agregado simples: só se altera pelos métodos dela.
/// </summary>
public sealed class Category : Entity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private Category(Guid id, string name, string? description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    // EF Core
    private Category()
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public static Category Create(string name, string? description)
    {
        var category = new Category(Guid.NewGuid(), string.Empty, null);
        category.Update(name, description);
        return category;
    }

    public void Update(string name, string? description)
    {
        Name = Guard.RequiredText(name, NameMaxLength, "Category.Name");
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Category.Description");
    }
}
