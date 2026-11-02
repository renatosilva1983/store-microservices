using BuildingBlocks.Application;
using BuildingBlocks.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Application.Shipping;

namespace Ordering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddHandlerDecorators();
        services.TryAddSingleton(TimeProvider.System);

        // Estratégias sem estado → Singleton. Todas registradas na mesma interface;
        // o ShippingCalculator recebe a coleção (IEnumerable<IShippingStrategy>).
        services.AddSingleton<IShippingStrategy, StandardShipping>();
        services.AddSingleton<IShippingStrategy, ExpressShipping>();
        services.AddSingleton<IShippingStrategy, PickupInStoreShipping>();
        services.AddSingleton<IShippingCalculator, ShippingCalculator>();

        return services;
    }
}
