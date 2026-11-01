using BuildingBlocks.Messaging;
using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Queries;
using Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Variável de ambiente com a conexão-base do PostgreSQL (host, porta, usuário e senha).
    /// A senha nunca fica no repositório. Cada serviço troca só o nome do banco.
    /// </summary>
    public const string ConnectionVariable = "POSTGRES_CONNECTION";

    public const string DefaultDatabaseName = "catalog";

    /// <summary>
    /// Liga as portas da Application às implementações desta camada (ADR-20).
    /// A Api chama só esta linha: é o único ponto de contato com a Infrastructure.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildConnectionString(configuration);

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema)));

        // Scoped: uma instância por requisição, compartilhada por repositórios e Unit of Work.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICatalogQueries, CatalogQueries>();

        // Mensageria: consome OrderCreated, baixa o estoque e responde com StockReserved/StockRejected.
        services.AddMessaging<CatalogDbContext>(configuration, "catalog", bus =>
            bus.AddConsumer<OrderCreatedConsumer>());

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
