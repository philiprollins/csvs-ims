using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Part.Queries;

public class GetPartBySkuQuery : IQuery<Result<GetPartBySkuResult>>
{
    public PartSku Sku { get; set; } = null!;

    private GetPartBySkuQuery() { }

    public static Result<GetPartBySkuQuery> Create(string sku)
    {
        var skuResult = PartSku.Create(sku);
        if (skuResult.IsFailure)
            return Result.Fail<GetPartBySkuQuery>(skuResult.Errors);

        return Result.Ok(new GetPartBySkuQuery
        {
            Sku = skuResult.Value
        });
    }
}

public class GetPartBySkuResult
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUri { get; set; }
    public List<PartTransactionDto> Transactions { get; set; } = new();
}

public class PartTransactionDto
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

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