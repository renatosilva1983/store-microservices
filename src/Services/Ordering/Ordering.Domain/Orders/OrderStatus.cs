namespace Ordering.Domain.Orders;

public enum OrderStatus
{
    /// <summary>Criado; aguardando a baixa de estoque (Fase 3, via evento).</summary>
    Pending,
    Confirmed,
    Cancelled
}
