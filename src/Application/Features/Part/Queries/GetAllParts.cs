using Library;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Part.Queries;

public sealed record GetAllPartsQuery(int Page, int PageSize) : IQuery<GetAllPartsResult>
{
    public static Result<GetAllPartsQuery> Create(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        return Result.Ok(new GetAllPartsQuery(
            page,
            pageSize
        ));
    }
}

public class GetAllPartsResult
{
    public List<PartSummaryDto> Items { get; set; } = [];
    public PaginationMeta Meta { get; set; } = new();
}

public class PartSummaryDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUri { get; set; }
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class GetAllPartsQueryHandler(PartsDbContext dbContext)
    : IQueryHandler<GetAllPartsQuery, GetAllPartsResult>
{
    public async Task<GetAllPartsResult> HandleAsync(GetAllPartsQuery query, CancellationToken cancellationToken)
    {
        var totalItems = await dbContext.PartSummary.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);

        var parts = await dbContext.PartSummary
            .OrderBy(p => p.Sku)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = parts.Select(p => new PartSummaryDto
        {
            Sku = p.Sku,
            Name = p.Name,
            Quantity = p.Quantity,
            SourceName = string.IsNullOrEmpty(p.SourceName) ? null : p.SourceName,
            SourceUri = string.IsNullOrEmpty(p.SourceUri) ? null : p.SourceUri
        }).ToList();

        return new GetAllPartsResult
        {
            Items = items,
            Meta = new PaginationMeta
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }
}