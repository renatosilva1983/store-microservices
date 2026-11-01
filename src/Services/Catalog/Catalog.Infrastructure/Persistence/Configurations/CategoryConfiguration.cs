using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasMaxLength(Category.NameMaxLength)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(Category.DescriptionMaxLength);

        builder.HasIndex(category => category.Name).IsUnique();

        // Seed das categorias iniciais (vira parte da migration).
        builder.HasData(
            new { Id = SeedData.ElectronicsId, Name = "Eletrônicos", Description = (string?)"Celulares, notebooks e acessórios" },
            new { Id = SeedData.BooksId, Name = "Livros", Description = (string?)"Livros técnicos e de ficção" },
            new { Id = SeedData.HomeId, Name = "Casa", Description = (string?)"Utilidades domésticas" });
    }
}

internal static class SeedData
{
    public static readonly Guid ElectronicsId = Guid.Parse("8c5d2b52-8a2b-4f4e-9d6a-1b8f0c3e2a01");
    public static readonly Guid BooksId = Guid.Parse("8c5d2b52-8a2b-4f4e-9d6a-1b8f0c3e2a02");
    public static readonly Guid HomeId = Guid.Parse("8c5d2b52-8a2b-4f4e-9d6a-1b8f0c3e2a03");
}
