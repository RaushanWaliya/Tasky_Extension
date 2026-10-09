using Tasky.Api.Domain.Entities;

namespace Tasky.Api.Repositories;

public interface IApiCallLogRepository
{
    Task AddAsync(ApiCallLog log, CancellationToken ct);
    Task<List<ApiCallLog>> GetRecentAsync(int take, CancellationToken ct);
}
