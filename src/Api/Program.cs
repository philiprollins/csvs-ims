using System.Reflection;
using Application;
using Library;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Api.Endpoints;
using Api.Errors;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add core services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "csvs-ims API", Version = "v1" });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// DbContexts (swap providers in tests)
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<PartsDbContext>(options => options.UseInMemoryDatabase("parts-tests"));
    builder.Services.AddDbContext<EventStoreDbContext>(options => options.UseInMemoryDatabase("events-tests"));
}
else
{
    var connStr = builder.Configuration.GetConnectionString("PartsDb");
    builder.Services.AddDbContext<PartsDbContext>(options => options.UseNpgsql(connStr));
    builder.Services.AddDbContext<EventStoreDbContext>(options => options.UseNpgsql(connStr));
}

// Application wiring (dispatchers, handlers, event store, projections)
var appAssembly = typeof(Application.Features.Part.Commands.DefinePartCommand).Assembly;
var valueObjectsAssembly = appAssembly; // contains JSON converters for VOs

builder.Services
    .AddInMemoryEventBus()
    .AddAggregateRepository()
    .AddEventStore(valueObjectsAssembly)
    .AddApplication(appAssembly)
    .RegisterProjections(appAssembly);

var app = builder.Build();

// Apply EF migrations at startup (skip in testing)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var partsDb = scope.ServiceProvider.GetRequiredService<PartsDbContext>();
    if (partsDb.Database.IsRelational())
        await partsDb.Database.MigrateAsync();

    var eventStoreDb = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    if (eventStoreDb.Database.IsRelational())
        await eventStoreDb.Database.MigrateAsync();
}

// Pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "csvs-ims API v1");
        c.DocumentTitle = "csvs-ims API";
    });
}

// API v1 group
var apiV1 = app.MapGroup("/api/v1").WithGroupName("v1");

// Feature slices
apiV1.MapPartsEndpoints();

app.Run();

// Expose Program for WebApplicationFactory in tests
public partial class Program { }
