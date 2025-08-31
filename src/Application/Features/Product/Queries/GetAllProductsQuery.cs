using Application.Features.Product.Projections;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Product.Queries;

public class GetAllProductsQuery : IQuery<Result<List<ProductSummaryReadModel>>> { }

public class GetAllProductsQueryHandler(PartsDbContext dbContext)
    : IQueryHandler<GetAllProductsQuery, Result<List<ProductSummaryReadModel>>>
{
    public async Task<Result<List<ProductSummaryReadModel>>> HandleAsync(GetAllProductsQuery query, CancellationToken cancellationToken = default)
    {
        var products = await dbContext.ProductSummary
            .OrderBy(p => p.Sku)
            .ToListAsync(cancellationToken);

        return Result.Ok(products);
    }
}

public class GetProductBySkuQuery : IQuery<Result<ProductDetailReadModel?>>
{
    public string Sku { get; set; } = string.Empty;

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