using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Persistence.Queries;
using Ordering.Infrastructure.Persistence.Repositories;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "OrderingDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' was not found.");

        services.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", OrderingDbContext.Schema)));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrderingDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderQueries, OrderQueries>();

        return services;
    }
}
