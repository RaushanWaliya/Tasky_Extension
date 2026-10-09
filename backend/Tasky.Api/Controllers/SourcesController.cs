using Microsoft.AspNetCore.Mvc;
using Tasky.Api.Dtos;
using Tasky.Api.Services;

namespace Tasky.Api.Controllers;

[ApiController]
[Route("api/sources")]
public class SourcesController : ControllerBase
{
    private readonly ISourceCatalogService _catalog;
    private readonly IProxyService _proxy;

    public SourcesController(ISourceCatalogService catalog, IProxyService proxy)
    {
        _catalog = catalog;
        _proxy = proxy;
    }

    /// <summary>The list of tabs for the extension.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ApiSourceDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _catalog.GetAllAsync(ct));
    }

    /// <summary>Runs one tab's API with the inputs the user typed (works for GET and POST sources).</summary>
    [HttpPost("{key}/execute")]
    public async Task<ActionResult<ExecuteResponseDto>> Execute(
        string key, [FromBody] ExecuteRequestDto? request, CancellationToken ct)
    {
        var clientHost = Request.Headers.Origin.ToString();
        var result = await _proxy.ExecuteAsync(key, request ?? new ExecuteRequestDto(), clientHost, ct);
        return Ok(result);
    }
}
