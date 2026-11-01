namespace Catalog.Application.Abstractions;

/// <summary>
/// Unit of Work (ADR-14): confirma de uma vez tudo o que os repositórios alteraram.
/// O DbContext do EF Core já é um Unit of Work; esta interface só o expõe sem vazar o EF.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
