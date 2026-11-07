namespace Ordering.Api.Contracts;

/// <summary>
/// Sem nome nem e-mail do cliente: quem compra é o usuário do token.
/// Aceitar esses campos aqui deixaria alguém criar pedido em nome de outra pessoa.
/// </summary>
public sealed record CreateOrderRequest(
    string ShippingMethod,
    IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
