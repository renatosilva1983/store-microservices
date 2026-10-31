using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Name)
            .HasMaxLength(Product.NameMaxLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(Product.DescriptionMaxLength);

        // Value Object de um valor só → conversão para coluna simples.
        builder.Property(product => product.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value))
            .HasMaxLength(Sku.MaxLength)
            .IsRequired();

        builder.HasIndex(product => product.Sku).IsUnique();

        // Value Object de vários valores → complex type (colunas na própria tabela, sem identidade).
        builder.ComplexProperty(product => product.Price, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(money => money.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(Money.CurrencyLength)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Property(product => product.StockQuantity).IsRequired();
        builder.Property(product => product.CreatedAtUtc).IsRequired();

        // Agregados se referenciam por Id: FK sem propriedade de navegação no domínio.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(product => product.Name);
    }
}
