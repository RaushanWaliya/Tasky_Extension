using Microsoft.EntityFrameworkCore;
using Tasky.Api.Data;
using Tasky.Api.Domain.Entities;

namespace Tasky.Api.Repositories;

public class ApiCallLogRepository : IApiCallLogRepository
{
    private readonly TaskyDbContext _db;

    public ApiCallLogRepository(TaskyDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ApiCallLog log, CancellationToken ct)
    {
        await _db.ApiCallLogs.AddAsync(log, ct);
    }

    public Task<List<ApiCallLog>> GetRecentAsync(int take, CancellationToken ct)
    {
        return _db.ApiCallLogs
            .AsNoTracking()
            .Include(x => x.ApiSource)
            .OrderByDescending(x => x.RequestedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(ct);
    }
}
