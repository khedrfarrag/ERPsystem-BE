using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Shared;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SeedController : ControllerBase
{
    private readonly IDemoDataSeeder _seeder;

    public SeedController(IDemoDataSeeder seeder)
    {
        _seeder = seeder;
    }

    [HttpPost("demo-store")]
    [ProducesResponseType(typeof(ApiResponse<DemoSeedResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedDemoStore([FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        var result = await _seeder.SeedDemoDataAsync(force, cancellationToken);
        return Ok(ApiResponse<DemoSeedResult>.Ok(result, result.Message));
    }
}
