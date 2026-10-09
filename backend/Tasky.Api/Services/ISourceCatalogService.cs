using Tasky.Api.Dtos;

namespace Tasky.Api.Services;

public interface ISourceCatalogService
{
    Task<List<ApiSourceDto>> GetAllAsync(CancellationToken ct);
    Task<List<CallLogDto>> GetRecentLogsAsync(int take, CancellationToken ct);
}
