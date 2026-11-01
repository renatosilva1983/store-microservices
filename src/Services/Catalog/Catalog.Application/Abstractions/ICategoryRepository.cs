using Catalog.Domain.Categories;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Porta de saída do agregado Categoria (lado de escrita).
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken);

    void Add(Category category);

    void Remove(Category category);
}
