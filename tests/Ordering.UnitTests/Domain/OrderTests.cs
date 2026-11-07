using Ordering.Domain.Common;
using Ordering.Domain.Orders;

namespace Ordering.UnitTests.Domain;

public class OrderTests
{
    private static readonly Guid Notebook = Guid.NewGuid();
    private static readonly Guid Mouse = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 1, 15, 0, 0, DateTimeKind.Utc);

    private static Order NewOrder() => Order.Create("renato@example.com", "Renato", "Renato@Example.com", Now);

    [Fact]
    public void Create_NormalizesEmailAndStartsPending()
    {
        var order = NewOrder();

        order.CustomerEmail.ShouldBe("renato@example.com");
        order.Status.ShouldBe(OrderStatus.Pending);
        order.Currency.ShouldBe("BRL");
        order.Items.ShouldBeEmpty();
    }

    [Fact]
    public void AddItem_CalculatesSubtotalAndTotal()
    {
        var order = NewOrder();

        order.AddItem(Notebook, "Notebook", "NB-001", 4000m, 1);
        order.AddItem(Mouse, "Mouse", "MS-001", 100m, 2);
        order.SetShipping(ShippingMethod.Express, 51m);

        order.Subtotal.ShouldBe(4200m);
        order.ShippingCost.ShouldBe(51m);
        order.Total.ShouldBe(4251m);
        order.TotalQuantity.ShouldBe(3);
    }

    [Fact]
    public void AddItem_SameProductTwice_MergesQuantity()
    {
        var order = NewOrder();

        order.AddItem(Mouse, "Mouse", "MS-001", 100m, 2);
        order.AddItem(Mouse, "Mouse", "MS-001", 100m, 3);

        order.Items.Count.ShouldBe(1);
        order.Items.Single().Quantity.ShouldBe(5);
        order.Subtotal.ShouldBe(500m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void AddItem_WithInvalidQuantity_Throws(int quantity)
    {
        Should.Throw<DomainException>(() => NewOrder().AddItem(Mouse, "Mouse", "MS-001", 100m, quantity))
            .Code.ShouldBe("Order.InvalidQuantity");
    }

    [Fact]
    public void Confirm_WithoutItems_Throws()
    {
        Should.Throw<DomainException>(() => NewOrder().Confirm())
            .Code.ShouldBe("Order.Empty");
    }

    [Fact]
    public void Cancel_SetsStatusAndDate_AndBlocksChanges()
    {
        var order = NewOrder();
        order.AddItem(Mouse, "Mouse", "MS-001", 100m, 1);

        order.Cancel(Now);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancelledAtUtc.ShouldBe(Now);
        Should.Throw<DomainException>(() => order.AddItem(Notebook, "Notebook", "NB-001", 1m, 1))
            .Code.ShouldBe("Order.NotPending");
    }

    [Fact]
    public void Cancel_Twice_Throws()
    {
        var order = NewOrder();
        order.Cancel(Now);

        Should.Throw<DomainException>(() => order.Cancel(Now))
            .Code.ShouldBe("Order.AlreadyCancelled");
    }
}
