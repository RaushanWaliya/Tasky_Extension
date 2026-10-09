using System.Text.Json;
using Tasky.Api.Domain.Entities;
using Tasky.Api.Dtos;

namespace Tasky.Api.Mapping;

/// <summary>Manual mapping (no AutoMapper) so you can see exactly what goes where.</summary>
public static class MappingExtensions
{
    public static ApiSourceDto ToDto(this ApiSource source)
    {
        return new ApiSourceDto
        {
            Key = source.Key,
            Title = source.Title,
            Icon = source.Icon,
            Description = source.Description,
            HttpMethod = source.HttpMethod,
            DisplayType = source.DisplayType,
            DisplayConfig = ParseJson(source.DisplayConfigJson, "{}"),
            Inputs = ParseJson(source.InputSchemaJson, "[]"),
            CacheSeconds = source.CacheSeconds
        };
    }

    public static CallLogDto ToDto(this ApiCallLog log)
    {
        return new CallLogDto
        {
            Id = log.Id,
            SourceKey = log.ApiSource?.Key ?? string.Empty,
            RequestedAtUtc = log.RequestedAtUtc,
            DurationMs = log.DurationMs,
            StatusCode = log.StatusCode,
            IsSuccess = log.IsSuccess,
            FromCache = log.FromCache,
            ErrorMessage = log.ErrorMessage
        };
    }

    private static JsonElement ParseJson(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? fallback : json);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var doc = JsonDocument.Parse(fallback);
            return doc.RootElement.Clone();
        }
    }
}
