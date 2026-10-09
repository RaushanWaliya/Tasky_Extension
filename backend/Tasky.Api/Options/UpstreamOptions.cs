namespace Tasky.Api.Options;

/// <summary>Settings for calls to external APIs (section "Upstream" in appsettings.json).</summary>
public class UpstreamOptions
{
    public const string SectionName = "Upstream";

    public int TimeoutSeconds { get; set; } = 15;
    public long MaxResponseBytes { get; set; } = 2 * 1024 * 1024;
    public string UserAgent { get; set; } = "TaskyExtension/1.0";
}
