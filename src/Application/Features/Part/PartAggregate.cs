using Application.Features.Part.ValueObjects;
using Library;

namespace Application.Features.Part;

public record PartDefinedEvent(PartSku Sku, PartName Name) : Event(Sku.Value);

public record PartAcquiredEvent(PartSku Sku, Quantity Quantity, string Justification) : Event(Sku.Value);

public record PartConsumedEvent(PartSku Sku, Quantity Quantity, string Justification) : Event(Sku.Value);

public record PartRecountedEvent(PartSku Sku, Quantity Quantity, string Justification) : Event(Sku.Value);

public record PartSourceUpdatedEvent(PartSku Sku, PartSource Source) : Event(Sku.Value);

public class PartAggregate : AggregateRoot
{
    public PartSku Sku { get; private set; } = null!;
    public PartName Name { get; private set; } = null!;
    public Quantity CurrentQuantity { get; private set; } = null!;
    public Maybe<PartSource> Source { get; private set; } = Maybe<PartSource>.None();

    public static Result<PartAggregate> Define(PartSku sku, PartName name)
    {
        var part = new PartAggregate();
        part.RaiseEvent(new PartDefinedEvent(sku, name));
        return Result.Ok(part);
    }

    public Result<PartAggregate> Acquire(Quantity quantity, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            return Result.Fail<PartAggregate>("justification", "Justification is required for acquiring parts");

        var newQuantityResult = CurrentQuantity.Add(quantity);
        if (!newQuantityResult.IsSuccess)
            return Result.Fail<PartAggregate>(newQuantityResult.Errors);

        RaiseEvent(new PartAcquiredEvent(Sku, quantity, justification));

        return Result.Ok(this);
    }

    public Result<PartAggregate> Consume(Quantity quantity, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            return Result.Fail<PartAggregate>("justification", "Justification is required for consuming parts");

        var newQuantity = CurrentQuantity.Value - quantity.Value;
        if (newQuantity < 0)
            return Result.Fail<PartAggregate>("quantity", "Cannot consume more parts than are available");

        var newQuantityResult = CurrentQuantity.Subtract(quantity);
        if (!newQuantityResult.IsSuccess)
            return Result.Fail<PartAggregate>(newQuantityResult.Errors);

        RaiseEvent(new PartConsumedEvent(Sku, quantity, justification));

        return Result.Ok(this);
    }

    public Result<PartAggregate> Recount(Quantity newQuantity, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            return Result.Fail<PartAggregate>("justification", "Justification is required for recounting parts");

        RaiseEvent(new PartRecountedEvent(Sku, newQuantity, justification));

        return Result.Ok(this);
    }

    public Result<PartAggregate> UpdateSource(PartSource source)
    {
        RaiseEvent(new PartSourceUpdatedEvent(Sku, source));

        return Result.Ok(this);
    }

    protected override void Apply(Event @event)
    {
        switch (@event)
        {
            case PartDefinedEvent e:
                AggregateId = e.Sku.Value;
                Sku = e.Sku;
                Name = e.Name;
                CurrentQuantity = Quantity.Create(0).Value;
                break;

            case PartAcquiredEvent e:
                CurrentQuantity = CurrentQuantity.Add(e.Quantity).Value;
                break;

            case PartConsumedEvent e:
                CurrentQuantity = CurrentQuantity.Subtract(e.Quantity).Value;
                break;

            case PartRecountedEvent e:
                CurrentQuantity = e.Quantity;
                break;

            case PartSourceUpdatedEvent e:
                Source = Maybe<PartSource>.Some(e.Source);
                break;
        }
    }
}