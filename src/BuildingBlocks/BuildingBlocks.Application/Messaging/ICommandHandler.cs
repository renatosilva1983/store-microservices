using BuildingBlocks.Application.Results;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Caso de uso de escrita sem valor de retorno.
/// </summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Caso de uso de escrita que devolve um valor.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
