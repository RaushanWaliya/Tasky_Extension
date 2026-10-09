using Microsoft.AspNetCore.Mvc;
using Tasky.Api.Dtos;
using Tasky.Api.Services;

namespace Tasky.Api.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private readonly ISourceCatalogService _catalog;

    public LogsController(ISourceCatalogService catalog)
    {
        _catalog = catalog;
    }

    /// <summary>Most recent calls (newest first). Handy to see what the extension is doing.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CallLogDto>>> GetRecent([FromQuery] int take = 50, CancellationToken ct = default)
    {
        return Ok(await _catalog.GetRecentLogsAsync(take, ct));
    }
}
