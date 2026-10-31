using Catalog.Domain.Common;
using Catalog.Domain.ValueObjects;

namespace Catalog.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidValues_RoundsToTwoDecimalsAndNormalizesCurrency()
    {
        var money = Money.Create(10.005m, " brl ");

        money.Amount.ShouldBe(10.01m);
        money.Currency.ShouldBe("BRL");
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        var exception = Should.Throw<DomainException>(() => Money.Create(-1m));

        exception.Code.ShouldBe("Money.NegativeAmount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("RE")]
    [InlineData("REAL")]
    [InlineData("R$1")]
    public void Create_WithInvalidCurrency_ThrowsDomainException(string currency)
    {
        var exception = Should.Throw<DomainException>(() => Money.Create(1m, currency));

        exception.Code.ShouldBe("Money.InvalidCurrency");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Money.Create(5m).ShouldBe(Money.Create(5.00m));
    }
}
