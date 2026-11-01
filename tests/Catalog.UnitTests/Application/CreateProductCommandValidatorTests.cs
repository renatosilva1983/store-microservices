using Catalog.Application.Products.Create;

namespace Catalog.UnitTests.Application;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        var command = new CreateProductCommand("Notebook", null, "NB-001", 10m, 0, Guid.NewGuid());

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithInvalidFields_ReportsEachField()
    {
        var command = new CreateProductCommand("", null, "x", -1m, -1, Guid.Empty);

        var result = _validator.Validate(command);

        result.Errors.Select(error => error.PropertyName).Distinct().ShouldBe(
            new[] { "Name", "Price", "InitialStock", "CategoryId", "Sku" },
            ignoreOrder: true);
    }
}
