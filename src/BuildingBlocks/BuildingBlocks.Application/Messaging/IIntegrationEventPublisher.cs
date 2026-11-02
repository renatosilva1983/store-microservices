namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Porta para publicar eventos de integração. A Application não sabe que existe RabbitMQ
/// nem MassTransit: a implementação fica em BuildingBlocks.Messaging (Ports and Adapters).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
