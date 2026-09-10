using RetailOS.Api.Extensions;
using RetailOS.Api.Middleware;
using RetailOS.Application;
using RetailOS.Infrastructure;
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

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
