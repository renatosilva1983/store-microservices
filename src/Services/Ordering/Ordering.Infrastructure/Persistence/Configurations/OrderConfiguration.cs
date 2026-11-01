using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private const int EnumMaxLength = 20;

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.CustomerName).HasMaxLength(Order.CustomerNameMaxLength).IsRequired();
        builder.Property(order => order.CustomerEmail).HasMaxLength(Order.CustomerEmailMaxLength).IsRequired();

        // Enums gravados como texto: o banco fica legível e não quebra se a ordem do enum mudar.
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(order => order.ShippingMethod).HasConversion<string>().HasMaxLength(EnumMaxLength);

        builder.Property(order => order.Subtotal).HasPrecision(18, 2);
        builder.Property(order => order.ShippingCost).HasPrecision(18, 2);
        builder.Property(order => order.Total).HasPrecision(18, 2);
        builder.Property(order => order.Currency).HasMaxLength(3).IsFixedLength().IsRequired();

        builder.Ignore(order => order.TotalQuantity);
        builder.HasIndex(order => order.CustomerEmail);

        // Itens fazem parte do agregado: owned collection, salvos e carregados junto com o pedido.
        builder.OwnsMany(order => order.Items, items =>
        {
            items.ToTable("order_items");
            items.WithOwner().HasForeignKey("OrderId");
            items.HasKey(item => item.Id);
            items.Property(item => item.Id).ValueGeneratedNever();

            items.Property(item => item.ProductName).HasMaxLength(150).IsRequired();
            items.Property(item => item.Sku).HasMaxLength(20).IsRequired();
            items.Property(item => item.UnitPrice).HasPrecision(18, 2);
            items.Ignore(item => item.LineTotal);
        });

        // O EF usa o campo _items (a lista privada), não a propriedade somente leitura.
        builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
