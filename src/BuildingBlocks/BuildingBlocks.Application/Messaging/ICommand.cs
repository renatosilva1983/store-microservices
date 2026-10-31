namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Comando (escrita) sem valor de retorno — CQRS (ADR-12).
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Comando (escrita) que devolve um valor, por exemplo o Id criado.
/// </summary>
public interface ICommand<TResponse>
{
}
