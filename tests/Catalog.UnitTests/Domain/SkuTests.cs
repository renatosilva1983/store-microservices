using Catalog.Domain.Common;
using Catalog.Domain.ValueObjects;

namespace Catalog.UnitTests.Domain;

public class SkuTests
{
    [Fact]
    public void Create_NormalizesToUpperCaseWithoutSpaces()
    {
        var sku = Sku.Create("  nb-001 ");

        sku.Value.ShouldBe("NB-001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("SKU WITH SPACE")]
    [InlineData("SKU_UNDERSCORE")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void Create_WithInvalidFormat_ThrowsDomainException(string value)
    {
        var exception = Should.Throw<DomainException>(() => Sku.Create(value));

        exception.Code.ShouldBe("Sku.InvalidFormat");
    }

    [Theory]
    [InlineData("ABC", true)]
    [InlineData("nb-001", true)]
    [InlineData("A", false)]
    [InlineData(null, false)]
    public void IsValid_ReturnsExpected(string? value, bool expected)
    {
        Sku.IsValid(value).ShouldBe(expected);
    }
}
