namespace Library.Interfaces;

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