using Ordering.Application.Shipping;
using Ordering.Domain.Orders;

namespace Ordering.UnitTests.Application;

public class ShippingStrategyTests
{
    private readonly ShippingCalculator _calculator = new(
    [
        new StandardShipping(),
        new ExpressShipping(),
        new PickupInStoreShipping()
    ]);

    [Theory]
    [InlineData(299.99, 20)]
    [InlineData(300, 0)]
    [InlineData(1500, 0)]
    public void Standard_IsFreeFromThreshold(decimal subtotal, decimal expected)
    {
        _calculator.Calculate(ShippingMethod.Standard, new ShippingQuote(subtotal, 1)).ShouldBe(expected);
    }

    [Fact]
    public void Express_ChargesBaseFeePlusPerUnit()
    {
        _calculator.Calculate(ShippingMethod.Express, new ShippingQuote(1000m, 3)).ShouldBe(51m);
    }

    [Fact]
    public void PickupInStore_IsFree()
    {
        _calculator.Calculate(ShippingMethod.PickupInStore, new ShippingQuote(10m, 1)).ShouldBe(0m);
    }

    [Fact]
    public void Calculator_WithoutStrategyForMethod_Throws()
    {
        var calculator = new ShippingCalculator([new StandardShipping()]);

        Should.Throw<InvalidOperationException>(() =>
            calculator.Calculate(ShippingMethod.Express, new ShippingQuote(10m, 1)));
    }
}
