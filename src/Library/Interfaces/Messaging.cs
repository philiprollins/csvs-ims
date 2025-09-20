namespace Library.Interfaces;

/// <summary>
/// Marker interface for commands.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Defines a query.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface IQuery<TResult> { }

/// <summary>
/// Defines a handler for queries.
/// </summary>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handles the query asynchronously.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the query.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for commands.
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : Result
{
    /// <summary>
    /// Handles the command asynchronously.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of handling the command.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandDispatcher
{
    Task<Result> Send<TCommand>(TCommand command, CancellationToken ct)
        where TCommand : ICommand;
}

public interface IQueryDispatcher
{
    Task<TResponse> Send<TQuery, TResponse>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResponse>;
}