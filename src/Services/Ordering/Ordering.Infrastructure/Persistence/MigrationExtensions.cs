using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.Infrastructure.Persistence;

public static class MigrationExtensions
{
    public static async Task MigrateOrderingDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
