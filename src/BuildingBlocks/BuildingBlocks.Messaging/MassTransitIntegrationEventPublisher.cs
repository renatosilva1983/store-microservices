using BuildingBlocks.Application.Messaging;
using MassTransit;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Adaptador da porta IIntegrationEventPublisher.
/// Com o Bus Outbox ligado, o Publish NÃO vai direto para o RabbitMQ: a mensagem é gravada
/// na tabela de outbox, na mesma transação do SaveChanges do caso de uso (Outbox Pattern).
/// Um serviço em segundo plano entrega depois. Assim não existe "salvou o pedido, mas o evento se perdeu".
/// </summary>
internal sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
