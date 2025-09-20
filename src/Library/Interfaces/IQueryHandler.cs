namespace Library.Interfaces;

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