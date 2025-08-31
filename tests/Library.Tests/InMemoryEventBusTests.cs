using Library;
using Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Library.Tests;

public class TestEventHandler : IEventHandler<NameChangedEvent>
{
    public List<NameChangedEvent> HandledEvents { get; } = new();

    public Task HandleAsync(NameChangedEvent @event, CancellationToken cancellationToken = default)
    {
        HandledEvents.Add(@event);
        return Task.CompletedTask;
    }
}

public class InMemoryEventBusTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<InMemoryEventBus>> _loggerMock;
    private readonly InMemoryEventBus _eventBus;

    public InMemoryEventBusTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<InMemoryEventBus>>();
        _eventBus = new InMemoryEventBus(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DispatchAsync_CallsHandlers_WhenHandlersExist()
    {
        var handler = new TestEventHandler();
        var handlers = new List<object> { handler };
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventHandler<NameChangedEvent>>))).Returns(handlers);

        var @event = new NameChangedEvent("test-id", "New Name");

        await _eventBus.DispatchAsync(@event);

        Assert.Single(handler.HandledEvents);
        Assert.Equal(@event, handler.HandledEvents[0]);
    }

    [Fact]
    public async Task DispatchAsync_LogsDebug_WhenNoHandlers()
    {
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventHandler<NameChangedEvent>>))).Returns(new List<object>());

        var @event = new NameChangedEvent("test-id", "New Name");

        await _eventBus.DispatchAsync(@event);

        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task DispatchManyAsync_DispatchesAllEvents()
    {
        var handler = new TestEventHandler();
        var handlers = new List<object> { handler };
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventHandler<NameChangedEvent>>))).Returns(handlers);

        var events = new List<Event>
        {
            new NameChangedEvent("test-id1", "Name1"),
            new NameChangedEvent("test-id2", "Name2")
        };

        await _eventBus.DispatchManyAsync(events);

        Assert.Equal(2, handler.HandledEvents.Count);
    }
}