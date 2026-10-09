using System.Text.Json;

namespace Tasky.Api.Dtos;

public class ExecuteResponseDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime FetchedAtUtc { get; set; }
    public bool FromCache { get; set; }
    public int DurationMs { get; set; }
    public int UpstreamStatus { get; set; }
    public string UpstreamHost { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
}
