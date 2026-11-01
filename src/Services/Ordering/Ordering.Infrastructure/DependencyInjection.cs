using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure.Catalog;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Persistence.Queries;
using Ordering.Infrastructure.Persistence.Repositories;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Variável de ambiente com a conexão-base do PostgreSQL (host, porta, usuário e senha).
    /// A senha nunca fica no repositório. Cada serviço troca só o nome do banco.
    /// </summary>
    public const string ConnectionVariable = "POSTGRES_CONNECTION";

    public const string DefaultDatabaseName = "ordering";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildConnectionString(configuration);

        services.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", OrderingDbContext.Schema)));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrderingDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderQueries, OrderQueries>();

        // Integração com o Catálogo (HTTP + resiliência).
        services.AddCatalogClient(configuration);

        return services;
    }

    /// <summary>
    /// Monta a connection string a partir da variável de ambiente + o banco deste serviço
    /// (Database per Service: mesmo servidor, bancos separados).
    /// As variáveis de ambiente já fazem parte do IConfiguration no ASP.NET Core.
    /// </summary>
    private static string BuildConnectionString(IConfiguration configuration)
    {
        var baseConnection = configuration[ConnectionVariable];
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            throw new InvalidOperationException(
                $"Defina a variável de ambiente '{ConnectionVariable}' com a conexão do PostgreSQL " +
                "(ex.: Host=localhost;Port=5432;Username=postgres;Password=...).");
        }

        var builder = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = configuration["Database:Name"] ?? DefaultDatabaseName
        };

        return builder.ConnectionString;
    }
}
