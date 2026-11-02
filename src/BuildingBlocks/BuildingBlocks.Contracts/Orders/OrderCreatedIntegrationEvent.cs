namespace BuildingBlocks.Contracts.Orders;

/// <summary>
/// Evento de integração publicado pelo Ordering quando um pedido é criado.
/// Contratos são só dados (records), sem lógica: é o "idioma comum" entre serviços.
/// Mudança aqui afeta quem publica e quem consome — evolua adicionando campos, sem remover.
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderCreatedItem> Items);

public sealed record OrderCreatedItem(Guid ProductId, string Sku, int Quantity);
