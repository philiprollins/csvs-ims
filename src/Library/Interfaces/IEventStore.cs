namespace Library.Interfaces;

public interface IEventStore
{
    Task<Maybe<IEnumerable<Event>>> GetAllEvents();

    Task<Maybe<IEnumerable<Event>>> GetEventsForAggregateAsync(string aggregateId);

    Task<Result> SaveEventsAsync(string aggregateId, IEnumerable<Event> events, int expectedVersion);
}