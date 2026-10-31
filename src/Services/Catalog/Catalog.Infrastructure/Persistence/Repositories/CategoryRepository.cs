using Catalog.Application.Abstractions;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(CatalogDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.AnyAsync(category => category.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken) =>
        context.Categories.AnyAsync(
            category => category.Name.ToLower() == name.ToLower()
                        && (excludingId == null || category.Id != excludingId),
            cancellationToken);

    public void Add(Category category) => context.Categories.Add(category);

    public void Remove(Category category) => context.Categories.Remove(category);
}
