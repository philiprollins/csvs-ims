using Application.Features.Product.Projections;
using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Product.Queries;

public class GetAllProductsQuery : IQuery<Result<List<ProductSummaryReadModel>>> 
{
    private GetAllProductsQuery() { }

    public static Result<GetAllProductsQuery> Create()
    {
        return Result.Ok(new GetAllProductsQuery());
    }
}

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