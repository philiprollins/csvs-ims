namespace Library.Interfaces;

public interface IAggregateRepository<T> where T : AggregateRoot, new()
{
    Task<Maybe<T>> GetByIdAsync(string aggregateId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string aggregateId, CancellationToken cancellationToken = default);

    Task<Result> SaveAsync(T aggregate, CancellationToken cancellationToken = default);
}