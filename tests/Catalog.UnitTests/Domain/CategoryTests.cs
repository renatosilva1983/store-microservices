using Catalog.Domain.Categories;
using Catalog.Domain.Common;

namespace Catalog.UnitTests.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_TrimsNameAndDropsBlankDescription()
    {
        var category = Category.Create("  Informática ", "   ");

        category.Name.ShouldBe("Informática");
        category.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_WithTooLongName_ThrowsDomainException()
    {
        var name = new string('a', Category.NameMaxLength + 1);

        Should.Throw<DomainException>(() => Category.Create(name, null))
            .Code.ShouldBe("Category.Name.TooLong");
    }
}
