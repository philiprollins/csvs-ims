using Application;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PartsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PartsDb")));

builder.Services.AddDbContext<Library.EventStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PartsDb")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var partsDb = scope.ServiceProvider.GetRequiredService<PartsDbContext>();
    await partsDb.Database.MigrateAsync();
    
    var eventStoreDb = scope.ServiceProvider.GetRequiredService<Library.EventStoreDbContext>();
    await eventStoreDb.Database.MigrateAsync();
}

app.UseHttpsRedirection();


app.Run();
