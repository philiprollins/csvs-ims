using Application.Features.Part.Projections;
using Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Part.Queries;

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