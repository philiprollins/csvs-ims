using Application.Features.Product.Projections;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Product.Queries;

public class GetProductBySkuQuery : IQuery<Result<ProductDetailReadModel?>>
{
    public string Sku { get; set; } = string.Empty;

    private GetProductBySkuQuery() { }

    public static Result<GetProductBySkuQuery> Create(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return Result.Fail<GetProductBySkuQuery>("sku", "SKU cannot be empty");

        return Result.Ok(new GetProductBySkuQuery { Sku = sku });
    }
}

public class GetProductBySkuQueryHandler(PartsDbContext dbContext)
    : IQueryHandler<GetProductBySkuQuery, Result<ProductDetailReadModel?>>
{
    public async Task<Result<ProductDetailReadModel?>> HandleAsync(GetProductBySkuQuery query, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.ProductDetails
            .Include(p => p.PartTransactions.OrderByDescending(t => t.Timestamp))
            .SingleOrDefaultAsync(p => p.Sku == query.Sku, cancellationToken);

        return Result.Ok(product);
    }
}