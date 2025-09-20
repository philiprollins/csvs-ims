using Microsoft.AspNetCore.Http;

namespace Api.Filters;

public static class CorrelationIdFilter
{
    public const string HeaderName = "x-correlation-id";

    public static RouteHandlerBuilder WithCorrelationId(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var correlationId = GetOrCreateCorrelationId(http);
            http.Response.OnStarting(() =>
            {
                if (!http.Response.Headers.ContainsKey(HeaderName))
                    http.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });
            return await next(context);
        });

    public static RouteGroupBuilder WithCorrelationId(this RouteGroupBuilder group)
        => group.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var correlationId = GetOrCreateCorrelationId(http);
            http.Response.OnStarting(() =>
            {
                if (!http.Response.Headers.ContainsKey(HeaderName))
                    http.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });
            return await next(context);
        });

    private static string GetOrCreateCorrelationId(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue(HeaderName, out var values) && !string.IsNullOrWhiteSpace(values.ToString()))
        {
            var incoming = values.ToString().Trim();
            http.Items[HeaderName] = incoming;
            return incoming;
        }

        var generated = Guid.NewGuid().ToString();
        http.Items[HeaderName] = generated;
        return generated;
    }
}
