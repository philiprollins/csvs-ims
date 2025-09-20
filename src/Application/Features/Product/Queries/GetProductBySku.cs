using Application.Features.Part.ValueObjects;
using Application.Features.Product.Projections;
using Application.Features.Product.ValueObjects;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Product.Queries;

public sealed record GetProductBySkuQuery(ProductSku Sku) : IQuery<Result<ProductDetailReadModel?>>
{
    public static Result<GetProductBySkuQuery> Create(string sku)
    {
        var result = ProductSku.Create(sku);
        if (result.IsFailure)
            return Result.Fail<GetProductBySkuQuery>(result.Errors);

        return Result.Ok(new GetProductBySkuQuery(result.Value));
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