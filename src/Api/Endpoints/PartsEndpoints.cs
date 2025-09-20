using Api.Errors;
using Api.Filters;
using Application.Features.Part.Commands;
using Application.Features.Part.Queries;
using Library;
using Library.Interfaces;

namespace Api.Endpoints;

public static class PartsEndpoints
{
    public static RouteGroupBuilder MapPartsEndpoints(this RouteGroupBuilder apiV1)
    {
        var group = apiV1.MapGroup("/parts")
            .WithTags("Parts")
            .WithCorrelationId();

        // GET /api/v1/parts?page=&pageSize=
        group.MapGet("/", async (int? page, int? pageSize, IQueryDispatcher queries, HttpContext ctx, CancellationToken ct) =>
        {
            var qResult = GetAllPartsQuery.Create(page ?? 1, pageSize ?? 20);
            if (qResult.IsFailure)
                return qResult.ToValidationProblem(ctx);

            var result = await queries.Send<GetAllPartsQuery, GetAllPartsResult>(qResult.Value, ct);

            var response = new GetPartsResponse
            {
                Items = result.Items.Select(p => new PartListItem
                {
                    Sku = p.Sku,
                    Name = p.Name,
                    Quantity = p.Quantity,
                    SourceName = p.SourceName,
                    SourceUri = p.SourceUri
                }).ToList(),
                Meta = new PaginationResponse
                {
                    Page = result.Meta.Page,
                    PageSize = result.Meta.PageSize,
                    TotalItems = result.Meta.TotalItems,
                    TotalPages = result.Meta.TotalPages
                }
            };

            return Results.Ok(response);
        })
        .WithName("ListParts");

        // GET /api/v1/parts/{sku}
        group.MapGet("/{sku}", async (string sku, IQueryDispatcher queries, HttpContext ctx, CancellationToken ct) =>
        {
            var qResult = GetPartBySkuQuery.Create(sku);
            if (qResult.IsFailure)
                return qResult.ToValidationProblem(ctx);

            var result = await queries.Send<GetPartBySkuQuery, Result<GetPartBySkuResult>>(qResult.Value, ct);
            if (result.IsFailure)
                return result.ToProblem(ctx, StatusCodes.Status404NotFound, "Part not found");

            var p = result.Value;
            var response = new PartResponse
            {
                Sku = p.Sku,
                Name = p.Name,
                Quantity = p.Quantity,
                SourceName = p.SourceName,
                SourceUri = p.SourceUri,
                Transactions = p.Transactions.Select(t => new PartTransactionResponse
                {
                    Type = t.Type,
                    Quantity = t.Quantity,
                    Justification = t.Justification,
                    Timestamp = t.Timestamp
                }).ToList()
            };

            return Results.Ok(response);
        })
        .WithName("GetPartBySku");

        // POST /api/v1/parts
        group.MapPost("/", async (DefinePartRequest req, ICommandDispatcher commands, HttpContext ctx, CancellationToken ct) =>
        {
            var cmdResult = DefinePartCommand.Create(req.Sku, req.Name);
            if (cmdResult.IsFailure)
                return cmdResult.ToValidationProblem(ctx);

            var result = await commands.Send(cmdResult.Value, ct);
            if (result.IsFailure)
                return result.ToProblem(ctx, StatusCodes.Status409Conflict, "Conflict");

            var location = $"/api/v1/parts/{req.Sku.ToUpperInvariant()}";
            var response = new PartCreatedResponse(req.Sku.ToUpperInvariant(), req.Name);
            return Results.Created(location, response);
        })
        .WithName("CreatePart");

        // POST /api/v1/parts/{sku}/acquire
        group.MapPost("/{sku}/acquire", async (string sku, AcquirePartRequest req, ICommandDispatcher commands, HttpContext ctx, CancellationToken ct) =>
        {
            var cmdResult = AcquirePartCommand.Create(sku, req.Quantity, req.Justification);
            if (cmdResult.IsFailure)
                return cmdResult.ToValidationProblem(ctx);

            var result = await commands.Send(cmdResult.Value, ct);
            if (result.IsFailure)
                return result.ToProblem(ctx, StatusCodes.Status404NotFound, "Part not found");

            return Results.NoContent();
        })
        .WithName("AcquirePart");

        // POST /api/v1/parts/{sku}/consume
        group.MapPost("/{sku}/consume", async (string sku, ConsumePartRequest req, ICommandDispatcher commands, HttpContext ctx, CancellationToken ct) =>
        {
            var cmdResult = ConsumePartCommand.Create(sku, req.Quantity, req.Justification);
            if (cmdResult.IsFailure)
                return cmdResult.ToValidationProblem(ctx);

            var result = await commands.Send(cmdResult.Value, ct);
            if (result.IsFailure)
                return result.ToProblem(ctx, StatusCodes.Status404NotFound, "Part not found");

            return Results.NoContent();
        })
        .WithName("ConsumePart");

        // POST /api/v1/parts/{sku}/recount
        group.MapPost("/{sku}/recount", async (string sku, RecountPartRequest req, ICommandDispatcher commands, HttpContext ctx, CancellationToken ct) =>
        {
            var cmdResult = RecountPartCommand.Create(sku, req.Quantity, req.Justification);
            if (cmdResult.IsFailure)
                return cmdResult.ToValidationProblem(ctx);

            var result = await commands.Send(cmdResult.Value, ct);
            if (result.IsFailure)
                return result.ToProblem(ctx, StatusCodes.Status404NotFound, "Part not found");

            return Results.NoContent();
        })
        .WithName("RecountPart");

        // PUT /api/v1/parts/{sku}/source
        group.MapPut("/{sku}/source", async (string sku, UpdatePartSourceRequest req, ICommandDispatcher commands, HttpContext ctx, CancellationToken ct) =>
        {
            var cmdResult = UpdatePartSourceCommand.Create(sku, req.Name, req.Uri);
            if (cmdResult.IsFailure)
                return cmdResult.ToValidationProblem(ctx);

            var result = await commands.Send(cmdResult.Value, ct);
            if (result.IsFailure)
                return result.ToProblem(ctx, StatusCodes.Status404NotFound, "Part not found");

            return Results.NoContent();
        })
        .WithName("UpdatePartSource");

        return group;
    }
}

// DTOs
public sealed record DefinePartRequest(string Sku, string Name);
public sealed record PartCreatedResponse(string Sku, string Name);

public sealed record AcquirePartRequest(int Quantity, string Justification);
public sealed record ConsumePartRequest(int Quantity, string Justification);
public sealed record RecountPartRequest(int Quantity, string Justification);
public sealed record UpdatePartSourceRequest(string Name, string Uri);

public sealed class GetPartsResponse
{
    public List<PartListItem> Items { get; set; } = new();
    public PaginationResponse Meta { get; set; } = new();
}

public sealed class PartListItem
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUri { get; set; }
}

public sealed class PaginationResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class PartResponse
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUri { get; set; }
    public List<PartTransactionResponse> Transactions { get; set; } = new();
}

public sealed class PartTransactionResponse
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
