namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Consulta (leitura) — CQRS (ADR-12). Não altera estado.
/// </summary>
public interface IQuery<TResponse>
{
}
