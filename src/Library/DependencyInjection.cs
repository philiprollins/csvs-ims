using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Library;

public static class DependencyInjection
{
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services) =>
        services.AddSingleton<IEventBus, InMemoryEventBus>();

    public static IServiceCollection AddAggregateRepository(this IServiceCollection services) =>
        services.AddScoped(typeof(IAggregateRepository<>), typeof(AggregateRepository<>));

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
}