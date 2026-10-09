namespace Tasky.Api.Services.Proxy;

/// <summary>One input field of an API source (parsed from the input_schema_json column).</summary>
public class InputFieldDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>text | textarea | number | select</summary>
    public string Type { get; set; } = "text";

    public bool Required { get; set; }
    public string? Default { get; set; }

    /// <summary>Optional regular expression the value must match.</summary>
    public string? Pattern { get; set; }

    public int? MaxLength { get; set; }

    /// <summary>Allowed values when Type is "select".</summary>
    public List<string>? Options { get; set; }
}
