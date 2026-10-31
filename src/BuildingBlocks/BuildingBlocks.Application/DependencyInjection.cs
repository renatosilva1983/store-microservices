using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Envolve todos os handlers registrados com os decorators de validação e log (Scrutor).
    /// O último Decorate fica por fora: log → validação → handler.
    /// Deve ser chamado depois de registrar os handlers.
    /// </summary>
    public static IServiceCollection AddHandlerDecorators(this IServiceCollection services)
    {
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(ValidationDecorator.QueryHandler<,>));

        services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));

        return services;
    }
}
