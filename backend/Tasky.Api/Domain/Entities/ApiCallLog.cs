namespace Tasky.Api.Domain.Entities;

/// <summary>Audit trail: one row for every call the backend runs on behalf of the extension.</summary>
public class ApiCallLog
{
    public long Id { get; set; }
    public int ApiSourceId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public int DurationMs { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public bool FromCache { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientHost { get; set; }

    public ApiSource? ApiSource { get; set; }
}
