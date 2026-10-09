using Tasky.Api.Domain.Entities;

namespace Tasky.Api.Repositories;

public interface IApiSourceRepository
{
    Task<List<ApiSource>> GetActiveAsync(CancellationToken ct);
    Task<ApiSource?> GetActiveByKeyAsync(string key, CancellationToken ct);
}
