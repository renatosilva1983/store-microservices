namespace BuildingBlocks.Contracts.Catalog;

/// <summary>O Catálogo conseguiu baixar o estoque de todos os itens do pedido.</summary>
public sealed record StockReservedIntegrationEvent(Guid OrderId);

/// <summary>O Catálogo não conseguiu baixar o estoque (produto inexistente ou sem saldo).</summary>
public sealed record StockRejectedIntegrationEvent(Guid OrderId, string Reason);
