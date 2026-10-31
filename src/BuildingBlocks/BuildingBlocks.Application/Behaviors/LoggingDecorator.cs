using System.Diagnostics;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Decorator (ADR-13): registra início, fim, duração e erro de cada caso de uso.
/// Preocupação transversal fora do handler.
/// </summary>
public static class LoggingDecorator
{
    public sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TCommand).Name, () => inner.Handle(command, cancellationToken));
    }

    public sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        ILogger<CommandBaseHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> Handle(TCommand command, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TCommand).Name, () => inner.Handle(command, cancellationToken));
    }

    public sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> inner,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TQuery).Name, () => inner.Handle(query, cancellationToken));
    }

    private static async Task<TResult> LogAsync<TResult>(ILogger logger, string requestName, Func<Task<TResult>> next)
        where TResult : Result
    {
        logger.LogInformation("Processing {RequestName}", requestName);
        var startedAt = Stopwatch.GetTimestamp();

        var result = await next();

        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        if (result.IsSuccess)
        {
            logger.LogInformation("Completed {RequestName} in {ElapsedMs:0} ms", requestName, elapsedMs);
        }
        else
        {
            logger.LogWarning(
                "Completed {RequestName} with error {ErrorCode} in {ElapsedMs:0} ms",
                requestName,
                result.Error.Code,
                elapsedMs);
        }

        return result;
    }
}
