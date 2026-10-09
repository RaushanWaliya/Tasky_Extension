using Tasky.Api.Data;

namespace Tasky.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly TaskyDbContext _db;

    public UnitOfWork(TaskyDbContext db, IApiSourceRepository sources, IApiCallLogRepository callLogs)
    {
        _db = db;
        Sources = sources;
        CallLogs = callLogs;
    }

    public IApiSourceRepository Sources { get; }
    public IApiCallLogRepository CallLogs { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }

    public Task<bool> CanConnectAsync(CancellationToken ct)
    {
        return _db.Database.CanConnectAsync(ct);
    }
}
