using Tasky.Api.Dtos;
using Tasky.Api.Mapping;
using Tasky.Api.Repositories;

namespace Tasky.Api.Services;

/// <summary>Gives the extension the list of tabs, and the recent call history.</summary>
public class SourceCatalogService : ISourceCatalogService
{
    private readonly IUnitOfWork _uow;

    public SourceCatalogService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<ApiSourceDto>> GetAllAsync(CancellationToken ct)
    {
        var sources = await _uow.Sources.GetActiveAsync(ct);
        return sources.Select(s => s.ToDto()).ToList();
    }

    public async Task<List<CallLogDto>> GetRecentLogsAsync(int take, CancellationToken ct)
    {
        var safeTake = Math.Clamp(take, 1, 200);
        var logs = await _uow.CallLogs.GetRecentAsync(safeTake, ct);
        return logs.Select(l => l.ToDto()).ToList();
    }
}
