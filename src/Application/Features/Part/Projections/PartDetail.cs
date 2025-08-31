using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Part.Projections;

public class PartDetail
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public string SourceName { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    
    public List<PartTransaction> Transactions { get; set; } = new();
}

public class PartTransaction
{
    public int Id { get; set; }
    public string PartSku { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; } 
    public int QuantityBefore { get; set; } 
    public int QuantityAfter { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    public PartDetail Part { get; set; } = null!;
}

public class PartDetailProjection(PartsDbContext db) :
    IEventHandler<PartDefinedEvent>,
    IEventHandler<PartAcquiredEvent>,
    IEventHandler<PartConsumedEvent>,
    IEventHandler<PartRecountedEvent>,
    IEventHandler<PartSourceUpdatedEvent>
{
    public async Task HandleAsync(PartDefinedEvent @event, CancellationToken cancellationToken = default)
    {
        var existingPart = await db.PartDetails.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (existingPart == null)
        {
            var part = new PartDetail
            {
                Sku = @event.Sku.Value,
                Name = @event.Name.Value,
                Quantity = 0,
                CreatedAt = @event.Timestamp,
                LastModified = @event.Timestamp
            };
            
            db.PartDetails.Add(part);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PartAcquiredEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartDetails
            .Include(p => p.Transactions)
            .SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
            
        if (part != null)
        {
            var quantityBefore = part.Quantity;
            part.Quantity += @event.Quantity.Value;
            part.LastModified = @event.Timestamp;
            
            var transaction = new PartTransaction
            {
                PartSku = @event.Sku.Value,
                Type = "ACQUIRED",
                Quantity = @event.Quantity.Value,
                QuantityBefore = quantityBefore,
                QuantityAfter = part.Quantity,
                Justification = @event.Justification,
                Timestamp = @event.Timestamp,
                Part = part
            };
            
            part.Transactions.Add(transaction);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PartConsumedEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartDetails
            .Include(p => p.Transactions)
            .SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
            
        if (part != null)
        {
            var quantityBefore = part.Quantity;
            part.Quantity -= @event.Quantity.Value;
            part.LastModified = @event.Timestamp;
            
            var transaction = new PartTransaction
            {
                PartSku = @event.Sku.Value,
                Type = "CONSUMED",
                Quantity = -@event.Quantity.Value,
                QuantityBefore = quantityBefore,
                QuantityAfter = part.Quantity,
                Justification = @event.Justification,
                Timestamp = @event.Timestamp,
                Part = part
            };
            
            part.Transactions.Add(transaction);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PartRecountedEvent @event, CancellationToken cancellationToken = default)
{
    var part = await db.PartDetails
        .Include(p => p.Transactions)
        .SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        
    if (part != null)
    {
        var quantityBefore = part.Quantity;
        var quantityAfter = @event.Quantity.Value;
        var adjustment = quantityAfter - quantityBefore;
        
        part.Quantity = quantityAfter;
        part.LastModified = @event.Timestamp;
        
        var transaction = new PartTransaction
        {
            PartSku = @event.Sku.Value,
            Type = "RECOUNTED",
            Quantity = adjustment,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            Justification = @event.Justification,
            Timestamp = @event.Timestamp,
            Part = part
        };
        
        part.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }
}

    public async Task HandleAsync(PartSourceUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        var part = await db.PartDetails.SingleOrDefaultAsync(p => p.Sku == @event.Sku.Value, cancellationToken);
        if (part != null)
        {
            part.SourceName = @event.Source.Name;
            part.SourceUri = @event.Source.Uri;
            
            part.LastModified = @event.Timestamp;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
