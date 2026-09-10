using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailOS.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace RetailOS.IntegrationTests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;
    private string _connectionString = "Host=localhost;Port=5432;Database=retailos_test;Username=postgres;Password=98865113";
    private bool _isUsingContainer = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<RetailOS.Infrastructure.Persistence.Interceptors.TenantSaveChangesInterceptor>();
                options.UseNpgsql(_connectionString)
                       .UseSnakeCaseNamingConvention()
                       .AddInterceptors(interceptor);
            });
        });
    }

    private static bool _migrated = false;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public async Task InitializeAsync()
    {
        try
        {
            _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("retailos_test")
                .WithUsername("postgres")
                .WithPassword("98865113")
                .Build();

            await _dbContainer.StartAsync();
            _connectionString = _dbContainer.GetConnectionString();
            _isUsingContainer = true;
        }
        catch
        {
            // Docker unavailable; fallback to real local PostgreSQL 18
            _connectionString = "Host=localhost;Port=5432;Database=retailos_test;Username=postgres;Password=98865113";
            _isUsingContainer = false;
        }

        await _lock.WaitAsync();
        try
        {
            if (!_migrated)
            {
                using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using var cmd = new Npgsql.NpgsqlCommand("DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;", conn);
                    await cmd.ExecuteNonQueryAsync();
                }

                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                _migrated = true;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public new async Task DisposeAsync()
    {
        if (_isUsingContainer && _dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
    }
}
