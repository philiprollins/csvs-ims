namespace Library.Interfaces;

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand
    where TResult : Result
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}