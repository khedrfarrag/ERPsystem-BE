using RetailOS.Api.Extensions;
using RetailOS.Api.Middleware;
using RetailOS.Application;
using RetailOS.Infrastructure;
using RetailOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Global Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serilog Request Logging
app.UseSerilogRequestLogging();

// Swagger for API inspection & verification
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RetailOS API v1");
    c.RoutePrefix = "swagger";
});

if (app.Environment.IsDevelopment())
{
    // Auto-seed demo store on startup if not already created
    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<RetailOS.Application.Common.Interfaces.IDemoDataSeeder>();
        await seeder.SeedDemoDataAsync(force: false);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not auto-seed demo data on startup");
    }
}
else if (app.Environment.IsProduction())
{
    // 1. Auto-apply EF Core Migrations to Remote Neon Database on startup
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Log.Information("Applying EF Core migrations to remote database...");
        await db.Database.MigrateAsync();
        Log.Information("Database schema is up to date.");

        // 2. Idempotent production master data seeding (Units & Initial Owner only, no fake data)
        var prodSeeder = scope.ServiceProvider.GetRequiredService<RetailOS.Application.Common.Interfaces.IProductionDataSeeder>();
        await prodSeeder.SeedProductionMasterDataAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Could not apply migrations or seed production master data on startup");
    }
}

app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "Live", timestamp = DateTime.UtcNow }));
app.MapGet("/health/ready", async (RetailOS.Infrastructure.Persistence.AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "Ready", database = "Connected", timestamp = DateTime.UtcNow })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.Run();

public partial class Program { }
