using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Api.Tests;

public class PartsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PartsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePart_HappyPath_Returns201WithLocation()
    {
        var client = _factory.CreateClientWithCorrelation("abc-123");

        var req = new { sku = "ABC-123", name = "Widget" };
        var resp = await client.PostAsJsonAsync("/api/v1/parts", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location.Should().NotBeNull();
        resp.Headers.Location!.ToString().Should().Be("/api/v1/parts/ABC-123");

        var body = await resp.Content.ReadFromJsonAsync<PartCreatedResponse>();
        body.Should().NotBeNull();
        body!.Sku.Should().Be("ABC-123");
        body.Name.Should().Be("Widget");

        resp.Headers.Should().ContainKey("x-correlation-id");
        resp.Headers.GetValues("x-correlation-id").Should().ContainSingle().Which.Should().Be("abc-123");
    }

    [Fact]
    public async Task CreatePart_ValidationFailure_Returns422()
    {
        var client = _factory.CreateClient();

        var req = new { sku = "", name = "" };
        var resp = await client.PostAsJsonAsync("/api/v1/parts", req);

        resp.StatusCode.Should().Be((HttpStatusCode)422);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsLike>();
        problem.Should().NotBeNull();
        problem!.status.Should().Be(422);
        problem.errors.Should().NotBeNull();
        problem.errors!.Keys.Should().Contain("sku");
    }

    [Fact]
    public async Task GetPart_NotFound_Returns404()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/parts/DOES-NOT-EXIST");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsLike>();
        problem.Should().NotBeNull();
        problem!.status.Should().Be(404);
    }

    private sealed record PartCreatedResponse(string Sku, string Name);

    private sealed class ProblemDetailsLike
    {
        public string? title { get; set; }
        public int? status { get; set; }
        public string? detail { get; set; }
        public string? instance { get; set; }
        public Dictionary<string, string[]>? errors { get; set; }
    }
}
