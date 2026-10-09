using Microsoft.AspNetCore.Mvc;
using Tasky.Api.Repositories;

namespace Tasky.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public HealthController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>The extension calls this to show the green "connected" dot.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var databaseOk = false;
        try
        {
            databaseOk = await _uow.CanConnectAsync(ct);
        }
        catch (Exception)
        {
            databaseOk = false;
        }

        var body = new { status = databaseOk ? "ok" : "degraded", database = databaseOk, timeUtc = DateTime.UtcNow };
        return databaseOk ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
