using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var instance = httpContext.Request.Path.ToString();

        var problemDetails = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = exception.Message,
            Status = StatusCodes.Status500InternalServerError,
            Instance = instance,
        };

        problemDetails.Extensions["errors"] = new Dictionary<string, string[]>
        {
            { "unexpected", new[] { exception.Message } }
        };

        await Results.Problem(
            title: problemDetails.Title,
            detail: problemDetails.Detail,
            statusCode: problemDetails.Status,
            instance: problemDetails.Instance,
            extensions: problemDetails.Extensions
        ).ExecuteAsync(httpContext);

        return true;
    }
}
