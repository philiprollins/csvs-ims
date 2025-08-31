using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Part.Projections;

public class PartSummary
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public string SourceUri { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
}

public class PartSummaryProjection(PartsDbContext db) :
    IEventHandler<PartDefinedEvent>,
    IEventHandler<PartAcquiredEvent>,
    IEventHandler<PartConsumedEvent>,
    IEventHandler<PartRecountedEvent>,
    IEventHandler<PartSourceUpdatedEvent>
{
    public async Task HandleAsync(PartDefinedEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartSummary.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (part == null)
        {
            part = new PartSummary { Sku = @event.Sku.Value };
            db.PartSummary.Add(part);
        }
        part.Name = @event.Name;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(PartAcquiredEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartSummary.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (part != null)
        {
            part.Quantity += @event.Quantity.Value;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PartConsumedEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartSummary.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (part != null)
        {
            part.Quantity -= @event.Quantity.Value;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PartRecountedEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartSummary.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (part != null)
        {
            part.Quantity = @event.Quantity.Value;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PartSourceUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartSummary.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (part != null)
        {
            part.SourceUri = @event.Source.Uri;
            part.SourceName = @event.Source.Name;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
