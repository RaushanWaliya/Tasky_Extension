using Microsoft.EntityFrameworkCore;
using Tasky.Api.Data;
using Tasky.Api.Domain.Entities;

namespace Tasky.Api.Repositories;

public class ApiSourceRepository : IApiSourceRepository
{
    private readonly TaskyDbContext _db;

    public ApiSourceRepository(TaskyDbContext db)
    {
        _db = db;
    }

    public Task<List<ApiSource>> GetActiveAsync(CancellationToken ct)
    {
        return _db.ApiSources
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);
    }

    public Task<ApiSource?> GetActiveByKeyAsync(string key, CancellationToken ct)
    {
        return _db.ApiSources
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.Key == key, ct);
    }
}
