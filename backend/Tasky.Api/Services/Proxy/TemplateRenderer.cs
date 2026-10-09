using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tasky.Api.Services.Proxy;

/// <summary>Replaces {placeholders} in the URL and body templates with the validated values.</summary>
public static class TemplateRenderer
{
    private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>Values are URL-encoded, so a user can never break out of the query string.</summary>
    public static string RenderUrl(string template, IReadOnlyDictionary<string, string> values)
    {
        var url = Placeholder.Replace(template, match => Lookup(match, values, Uri.EscapeDataString));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("The URL template of this API source is not a valid http(s) URL.");
        }

        return uri.AbsoluteUri;
    }

    /// <summary>Values are JSON-escaped, so quotes and line breaks in the text cannot break the JSON.</summary>
    public static string RenderJsonBody(string template, IReadOnlyDictionary<string, string> values)
    {
        var body = Placeholder.Replace(template, match => Lookup(match, values, v => JsonEncodedText.Encode(v).ToString()));

        try
        {
            using var doc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("The body template of this API source does not produce valid JSON.");
        }

        return body;
    }

    private static string Lookup(Match match, IReadOnlyDictionary<string, string> values, Func<string, string> encode)
    {
        var name = match.Groups[1].Value;
        if (!values.TryGetValue(name, out var value))
        {
            throw new InvalidOperationException($"Template uses {{{name}}} but no input field with that name is defined.");
        }

        return encode(value);
    }
}
