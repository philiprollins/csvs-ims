using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Library.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInMemoryEventBus_RegistersIEventBusAsInMemoryEventBus()
    {
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging to satisfy InMemoryEventBus dependency

        services.AddInMemoryEventBus();

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetService<IEventBus>();

        Assert.IsType<InMemoryEventBus>(eventBus);
    }

    [Fact]
    public void AddAggregateRepository_RegistersIAggregateRepository()
    {
        var services = new ServiceCollection();
        var eventStoreMock = new Mock<IEventStore>();
        services.AddSingleton(eventStoreMock.Object); // Register mock IEventStore

        services.AddAggregateRepository();

        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetService<IAggregateRepository<TestAggregate>>();

        Assert.IsType<AggregateRepository<TestAggregate>>(repository);
    }

    [Fact]
    public void RegisterProjections_RegistersEventHandlers()
    {
        var services = new ServiceCollection();

        services.RegisterProjections(typeof(TestEventHandler).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var handlers = serviceProvider.GetServices<IEventHandler<NameChangedEvent>>();

        Assert.Single(handlers);
        Assert.IsType<TestEventHandler>(handlers.First());
    }

    [Fact]
    public void AddEventStore_RegistersIEventStoreAsEFCoreEventStore()
    {
        var services = new ServiceCollection();
        services.AddDbContext<EventStoreDbContext>(options => options.UseInMemoryDatabase("test")); // Add DbContext
        services.AddLogging();
        var eventBusMock = new Mock<IEventBus>();
        services.AddSingleton(eventBusMock.Object);

        services.AddEventStore(typeof(DependencyInjectionTests).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var eventStore = serviceProvider.GetService<IEventStore>();

        Assert.IsType<EFCoreEventStore>(eventStore);
    }

    [Fact]
    public void AddApplication_RegistersDispatchersAndHandlers()
    {
        var services = new ServiceCollection();

        services.AddApplication(typeof(TestCommandHandler).Assembly);

        var serviceProvider = services.BuildServiceProvider();
        var commandDispatcher = serviceProvider.GetService<ICommandDispatcher>();
        var queryDispatcher = serviceProvider.GetService<IQueryDispatcher>();

        Assert.IsType<CommandDispatcher>(commandDispatcher);
        Assert.IsType<QueryDispatcher>(queryDispatcher);
    }
}

// Test classes for handlers
public class TestCommand : ICommand { }

public class TestCommandHandler : ICommandHandler<TestCommand, Result>
{
    public Task<Result> HandleAsync(TestCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok());
    }
}

public class TestQuery : IQuery<string> { }

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult("test response");
    }
}