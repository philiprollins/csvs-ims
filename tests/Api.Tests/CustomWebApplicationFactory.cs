using System.Net.Http;
using System.Net.Http.Headers;
using Application;
using Library;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:PartsDb"] = "UseInMemoryDbForTests"
            }!;
            config.AddInMemoryCollection(inMemorySettings);
        });

        // No explicit service overrides needed; Program.cs switches to InMemory based on connection string.
    }

    public HttpClient CreateClientWithCorrelation(string? correlationId = null)
    {
        var client = CreateClient();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            client.DefaultRequestHeaders.Add("x-correlation-id", correlationId);
        }
        return client;
    }
}
