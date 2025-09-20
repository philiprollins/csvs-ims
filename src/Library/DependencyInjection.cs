using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Library;

/// <summary>
/// Provides extension methods for configuring dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the in-memory event bus to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services) =>
        services.AddSingleton<IEventBus, InMemoryEventBus>();

    /// <summary>
    /// Adds the aggregate repository to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAggregateRepository(this IServiceCollection services) =>
        services.AddScoped(typeof(IAggregateRepository<>), typeof(AggregateRepository<>));

    /// <summary>
    /// Registers event handlers from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for event handlers.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection RegisterProjections(this IServiceCollection services, Assembly assembly)
    {
        var handlerType = typeof(IEventHandler<>);

        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                .Select(i => (Handler: t, Interface: i)))
            .ToList();

        foreach (var (handler, @interface) in types)
        {
            services.AddTransient(@interface, handler);
        }

        return services;
    }

    /// <summary>
    /// Adds the event store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="valueObjectAssembly">The assembly containing value objects and converters.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddEventStore(this IServiceCollection services, Assembly valueObjectAssembly)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        var converterTypes = valueObjectAssembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(JsonConverter)) && !t.IsAbstract)
            .ToList();

        foreach (var converterType in converterTypes)
        {
            if (Activator.CreateInstance(converterType) is JsonConverter converter)
                jsonOptions.Converters.Add(converter);
        }

        services.AddSingleton(jsonOptions);
        services.AddScoped<IEventStore, EFCoreEventStore>();

        return services;
    }

    /// <summary>
    /// Registers dispatchers and auto-discovers all ICommandHandler<> and IQueryHandler<,> in the Application assembly.
    /// Call this once from the API (e.g., Program.cs).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, Assembly appAssembly)
    {
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        RegisterCommandHandlers(services, appAssembly);
        RegisterQueryHandlers(services, appAssembly);

        return services;
    }

    private static void RegisterCommandHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(ICommandHandler<,>);

        var handlers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => new { Service = i, Impl = t }));

        foreach (var h in handlers)
            services.AddScoped(h.Service, h.Impl);
    }

    private static void RegisterQueryHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(IQueryHandler<,>);

        var handlers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => new { Service = i, Impl = t }));

        foreach (var h in handlers)
            services.AddScoped(h.Service, h.Impl);
    }
}