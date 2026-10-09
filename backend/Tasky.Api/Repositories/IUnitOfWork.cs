namespace Tasky.Api.Repositories;

/// <summary>One place to reach every repository and to save all changes together.</summary>
public interface IUnitOfWork
{
    IApiSourceRepository Sources { get; }
    IApiCallLogRepository CallLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<bool> CanConnectAsync(CancellationToken ct);
}
