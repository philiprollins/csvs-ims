using Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Library;

public sealed class CommandDispatcher(IServiceProvider sp) : ICommandDispatcher
{
    public async Task<Result> Send<TCommand>(TCommand command, CancellationToken ct)
        where TCommand : ICommand
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand, Result>>();
        return await handler.HandleAsync(command, ct);
    }
}

public sealed class QueryDispatcher(IServiceProvider sp) : IQueryDispatcher
{
    public async Task<TResponse> Send<TQuery, TResponse>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResponse>
    {
        var handler = sp.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return await handler.HandleAsync(query, ct);
    }
}