using Application.Features.Part.Projections;
using Library.Interfaces;

namespace Application.Features.Part.Queries;

public class GetAllPartsQuery : IQuery<GetAllPartsResult>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public static GetAllPartsQuery Create(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // Max page size

        return new GetAllPartsQuery { Page = page, PageSize = pageSize };
    }
}

public class GetAllPartsResult
{
    public List<PartSummaryDto> Items { get; set; } = new();
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