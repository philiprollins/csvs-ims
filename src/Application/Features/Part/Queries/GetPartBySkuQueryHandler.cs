using Application.Features.Part.Projections;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Part.Queries;

public class GetPartBySkuQueryHandler(PartsDbContext dbContext)
    : IQueryHandler<GetPartBySkuQuery, Result<GetPartBySkuResult>>
{
    public async Task<Result<GetPartBySkuResult>> HandleAsync(GetPartBySkuQuery query, CancellationToken cancellationToken)
    {
        var part = await dbContext.PartDetails
            .Include(p => p.Transactions.OrderByDescending(t => t.Timestamp))
            .FirstOrDefaultAsync(p => p.Sku == query.Sku.Value, cancellationToken);

        if (part == null)
            return Result.Fail<GetPartBySkuResult>($"Part with SKU '{query.Sku.Value}' not found.");

        var transactions = part.Transactions
            .OrderByDescending(t => t.Timestamp)
            .Select(t => new PartTransactionDto
            {
                Type = t.Type,
                Quantity = t.Quantity,
                Justification = t.Justification,
                Timestamp = t.Timestamp
            }).ToList();

        var result = new GetPartBySkuResult
        {
            Sku = part.Sku,
            Name = part.Name,
            Quantity = part.Quantity,
            SourceName = string.IsNullOrEmpty(part.SourceName) ? null : part.SourceName,
            SourceUri = string.IsNullOrEmpty(part.SourceUri) ? null : part.SourceUri,
            Transactions = transactions
        };

        return Result.Ok(result);
    }
}