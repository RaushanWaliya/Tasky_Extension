namespace Tasky.Api.Dtos;

public class CallLogDto
{
    public long Id { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public int DurationMs { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public bool FromCache { get; set; }
    public string? ErrorMessage { get; set; }
}
