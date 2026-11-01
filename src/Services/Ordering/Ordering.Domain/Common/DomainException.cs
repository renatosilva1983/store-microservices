namespace Ordering.Domain.Common;

/// <summary>
/// Violação de invariante do domínio de Pedidos.
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
