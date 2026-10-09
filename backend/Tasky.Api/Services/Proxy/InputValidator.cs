using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tasky.Api.Exceptions;

namespace Tasky.Api.Services.Proxy;

/// <summary>
/// Checks what the user typed against the rules stored in the database.
/// Only fields declared in the schema are accepted, so nobody can sneak extra values into the upstream URL.
/// </summary>
public static class InputValidator
{
    private const int DefaultMaxLength = 200;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static List<InputFieldDefinition> ParseSchema(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<InputFieldDefinition>();
        }

        return JsonSerializer.Deserialize<List<InputFieldDefinition>>(json, JsonOptions)
               ?? new List<InputFieldDefinition>();
    }

    /// <summary>Returns the clean values (field name to text) or throws InputValidationException.</summary>
    public static Dictionary<string, string> Validate(
        IReadOnlyList<InputFieldDefinition> schema,
        IDictionary<string, string>? provided)
    {
        var clean = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var field in schema)
        {
            string value = string.Empty;
            if (provided != null && provided.TryGetValue(field.Name, out var supplied) && supplied != null)
            {
                value = supplied.Trim();
            }
            else if (field.Default != null)
            {
                value = field.Default.Trim();
            }

            var error = CheckField(field, value);
            if (error != null)
            {
                errors[field.Name] = error;
            }
            else
            {
                clean[field.Name] = value;
            }
        }

        if (errors.Count > 0)
        {
            throw new InputValidationException(errors);
        }

        return clean;
    }

    private static string? CheckField(InputFieldDefinition field, string value)
    {
        var label = string.IsNullOrWhiteSpace(field.Label) ? field.Name : field.Label;

        if (value.Length == 0)
        {
            return field.Required ? $"{label} is required." : null;
        }

        var maxLength = field.MaxLength ?? DefaultMaxLength;
        if (value.Length > maxLength)
        {
            return $"{label} must be at most {maxLength} characters.";
        }

        if (field.Type == "number"
            && !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return $"{label} must be a number.";
        }

        if (field.Type == "select"
            && (field.Options == null || !field.Options.Contains(value, StringComparer.Ordinal)))
        {
            return $"{label} must be one of the allowed options.";
        }

        if (!string.IsNullOrEmpty(field.Pattern))
        {
            var regex = new Regex(field.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
            if (!regex.IsMatch(value))
            {
                return $"{label} has an invalid format.";
            }
        }

        return null;
    }
}
