using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            status = "Live",
            timestamp = DateTime.UtcNow
        }, "Service is live"));
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        if (canConnect)
        {
            return Ok(ApiResponse<object>.Ok(new
            {
                status = "Ready",
                database = "Connected",
                timestamp = DateTime.UtcNow
            }, "Service is ready"));
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            ApiResponse<object>.Fail("Database connection unavailable", "DATABASE_UNAVAILABLE"));
    }
}
