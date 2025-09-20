using Library;
using Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Library.Tests;

public class DispatchersTests
{
    [Fact]
    public async Task CommandDispatcher_Send_CallsHandlerAndReturnsResult()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var handlerMock = new Mock<ICommandHandler<TestCommand, Result>>();
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        serviceProviderMock.Setup(sp => sp.GetService(typeof(ICommandHandler<TestCommand, Result>)))
            .Returns(handlerMock.Object);

        var dispatcher = new CommandDispatcher(serviceProviderMock.Object);
        var command = new TestCommand();

        var result = await dispatcher.Send(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        handlerMock.Verify(h => h.HandleAsync(command, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task QueryDispatcher_Send_CallsHandlerAndReturnsResponse()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        var handlerMock = new Mock<IQueryHandler<TestQuery, string>>();
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("response");

        serviceProviderMock.Setup(sp => sp.GetService(typeof(IQueryHandler<TestQuery, string>)))
            .Returns(handlerMock.Object);

        var dispatcher = new QueryDispatcher(serviceProviderMock.Object);
        var query = new TestQuery();

        var result = await dispatcher.Send<TestQuery, string>(query, CancellationToken.None);

        Assert.Equal("response", result);
        handlerMock.Verify(h => h.HandleAsync(query, CancellationToken.None), Times.Once);
    }
}