using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.Persistence;

public static class MigrationExtensions
{
    /// <summary>
    /// Aplica as migrations pendentes na inicialização (usado em desenvolvimento e no docker-compose).
    /// Cria um escopo próprio porque o DbContext é Scoped e aqui ainda não existe requisição.
    /// </summary>
    public static async Task MigrateCatalogDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
