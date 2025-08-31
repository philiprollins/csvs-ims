using Application.Features.Part.Projections;
using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Queries;

public class GetPartBySkuQuery : IQuery<Result<GetPartBySkuResult>>
{
    public PartSku Sku { get; set; }

    private GetPartBySkuQuery(PartSku sku)
    {
        Sku = sku;
    }

    public static Result<GetPartBySkuQuery> Create(string sku)
    {
        var skuResult = PartSku.Create(sku);
        if (skuResult.IsFailure)
            return Result.Fail<GetPartBySkuQuery>(skuResult.Errors);

        return Result.Ok(new GetPartBySkuQuery(skuResult.Value));
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