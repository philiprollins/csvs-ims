using Library;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

public static class ResultToHttp
{
    public static IResult ToValidationProblem(this Result result, HttpContext httpContext, string? title = null)
        => BuildProblem(result, httpContext, StatusCodes.Status422UnprocessableEntity, title ?? "Validation failed");

    public static IResult ToProblem(this Result result, HttpContext httpContext, int statusCode, string title)
        => BuildProblem(result, httpContext, statusCode, title);

    public static IResult ToOk<T>(this Result<T> result)
        => Results.Ok(result.Value);

    private static IResult BuildProblem(Result result, HttpContext ctx, int statusCode, string title)
    {
        var errorsExt = GroupErrors(result.Errors);

        return Results.Problem(
            title: title,
            statusCode: statusCode,
            instance: ctx.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = errorsExt
            }
        );
    }

    private static IDictionary<string, string[]> GroupErrors(Dictionary<string, string> errors)
    {
        var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in errors)
        {
            var key = string.IsNullOrWhiteSpace(kvp.Key) ? "error" : kvp.Key;
            var parts = kvp.Value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<string>();
                dict[key] = list;
            }
            list.AddRange(parts);
        }
        return dict.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }
}
