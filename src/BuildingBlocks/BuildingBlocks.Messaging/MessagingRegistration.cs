using BuildingBlocks.Application.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging;

public static class MessagingRegistration
{
    /// <summary>
    /// Registra o MassTransit com RabbitMQ e o Outbox/Inbox transacional do EF Core (ADR-06).
    /// - Outbox: eventos publicados no caso de uso só saem depois do commit no banco.
    /// - Inbox: o consumidor guarda o MessageId processado; mensagem repetida é ignorada (consumidor idempotente).
    /// - Retry: falha temporária no consumidor tenta de novo; esgotou → fila _error (dead-letter).
    /// </summary>
    public static IServiceCollection AddMessaging<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<IBusRegistrationConfigurator> configureConsumers)
        where TDbContext : DbContext
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password),
                "Defina a senha do RabbitMQ na variável de ambiente 'RabbitMq__Password'.")
            .ValidateOnStart();

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.AddMassTransit(bus =>
        {
            // Filas com o nome do serviço na frente: catalog-order-created, ordering-stock-reserved...
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, false));

            configureConsumers(bus);

            bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseMessageRetry(retry => retry.Intervals(500, 2000, 5000));
                endpoint.UseEntityFrameworkOutbox<TDbContext>(context);
            });

            bus.UsingRabbitMq((context, rabbit) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                rabbit.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });
                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Tabelas do Outbox/Inbox no banco do serviço (entram na migration do serviço).
    /// </summary>
    public static ModelBuilder AddMessagingTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        return modelBuilder;
    }
}
