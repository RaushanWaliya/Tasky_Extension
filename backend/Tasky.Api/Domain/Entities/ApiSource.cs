namespace Tasky.Api.Domain.Entities;

/// <summary>
/// One row = one tab in the extension. It describes an external API:
/// where to call it, what the user can type in, and how the result is shown.
/// </summary>
public class ApiSource
{
    public int Id { get; set; }

    /// <summary>Short unique name used in URLs, e.g. "weather".</summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>"GET" or "POST".</summary>
    public string HttpMethod { get; set; } = "GET";

    /// <summary>Upstream URL. Parts like {city} are replaced with the user's (validated) input.</summary>
    public string UrlTemplate { get; set; } = string.Empty;

    /// <summary>JSON body for POST calls, with {placeholders}. Null for GET.</summary>
    public string? BodyTemplate { get; set; }

    /// <summary>Extra request headers as a JSON object.</summary>
    public string HeadersJson { get; set; } = "{}";

    /// <summary>JSON array describing the input fields (name, label, type, validation).</summary>
    public string InputSchemaJson { get; set; } = "[]";

    /// <summary>"list", "stats" or "keyvalue" - how the extension draws the result.</summary>
    public string DisplayType { get; set; } = "list";

    /// <summary>JSON telling the UI which fields of the response to show.</summary>
    public string DisplayConfigJson { get; set; } = "{}";

    /// <summary>How long a successful GET result is reused. 0 = never cache.</summary>
    public int CacheSeconds { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
