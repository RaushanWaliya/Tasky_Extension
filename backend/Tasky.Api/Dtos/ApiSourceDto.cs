using System.Text.Json;

namespace Tasky.Api.Dtos;

/// <summary>What the extension receives for every tab. Secrets such as the upstream URL are not included.</summary>
public class ApiSourceDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public string DisplayType { get; set; } = "list";
    public JsonElement DisplayConfig { get; set; }
    public JsonElement Inputs { get; set; }
    public int CacheSeconds { get; set; }
}
