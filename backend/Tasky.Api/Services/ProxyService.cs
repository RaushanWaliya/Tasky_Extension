using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Tasky.Api.Domain.Entities;
using Tasky.Api.Dtos;
using Tasky.Api.Exceptions;
using Tasky.Api.Repositories;
using Tasky.Api.Services.Proxy;

namespace Tasky.Api.Services;

/// <summary>
/// The heart of the backend. For one tab it:
///   1. loads the API definition from the database,
///   2. validates what the user typed,
///   3. builds the real URL (and body),
///   4. calls the external API (or reuses a cached answer),
///   5. writes an audit row and returns the data.
/// The browser never talks to the external API directly, so keys, headers and rate limits stay on the server.
/// </summary>
public class ProxyService : IProxyService
{
    private readonly IUnitOfWork _uow;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProxyService> _logger;

    public ProxyService(
        IUnitOfWork uow,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ProxyService> logger)
    {
        _uow = uow;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ExecuteResponseDto> ExecuteAsync(
        string key, ExecuteRequestDto request, string? clientHost, CancellationToken ct)
    {
        var source = await _uow.Sources.GetActiveByKeyAsync(key, ct)
                     ?? throw new NotFoundException($"API source '{key}' was not found.");

        var schema = InputValidator.ParseSchema(source.InputSchemaJson);
        var values = InputValidator.Validate(schema, request.Inputs);

        var url = TemplateRenderer.RenderUrl(source.UrlTemplate, values);
        var body = string.IsNullOrWhiteSpace(source.BodyTemplate)
            ? null
            : TemplateRenderer.RenderJsonBody(source.BodyTemplate, values);

        var method = new HttpMethod(source.HttpMethod);
        var canCache = method == HttpMethod.Get && source.CacheSeconds > 0;
        var cacheKey = BuildCacheKey(source.Key, values);
        var upstreamHost = new Uri(url).Host;
        var timer = Stopwatch.StartNew();

        if (canCache && _cache.TryGetValue(cacheKey, out CachedResult? cached) && cached != null)
        {
            timer.Stop();
            await WriteLogAsync(source, (int)timer.ElapsedMilliseconds, cached.Status, true, true, null, clientHost, ct);
            return BuildResponse(source, cached, upstreamHost, true, (int)timer.ElapsedMilliseconds);
        }

        try
        {
            var result = await CallUpstreamAsync(source, method, url, body, ct);
            timer.Stop();

            if (canCache)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(source.CacheSeconds));
            }

            await WriteLogAsync(source, (int)timer.ElapsedMilliseconds, result.Status, true, false, null, clientHost, ct);
            return BuildResponse(source, result, upstreamHost, false, (int)timer.ElapsedMilliseconds);
        }
        catch (UpstreamException ex)
        {
            timer.Stop();
            await WriteLogAsync(source, (int)timer.ElapsedMilliseconds, ex.UpstreamStatus, false, false, ex.Message, clientHost, ct);
            throw;
        }
    }

    private async Task<CachedResult> CallUpstreamAsync(
        ApiSource source, HttpMethod method, string url, string? body, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("upstream");
        using var message = new HttpRequestMessage(method, url);

        foreach (var header in ParseHeaders(source.HeadersJson))
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (body != null)
        {
            message.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await client.SendAsync(message, ct);
            var text = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new UpstreamException(
                    $"{source.Title} API answered with HTTP {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            return new CachedResult(ToJsonElement(text), (int)response.StatusCode, DateTime.UtcNow);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new UpstreamException($"{source.Title} API did not answer in time.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Upstream call failed for {Source}", source.Key);
            throw new UpstreamException($"{source.Title} API could not be reached.");
        }
    }

    private async Task WriteLogAsync(
        ApiSource source, int durationMs, int status, bool success, bool fromCache,
        string? error, string? clientHost, CancellationToken ct)
    {
        try
        {
            await _uow.CallLogs.AddAsync(new ApiCallLog
            {
                ApiSourceId = source.Id,
                RequestedAtUtc = DateTime.UtcNow,
                DurationMs = durationMs,
                StatusCode = status,
                IsSuccess = success,
                FromCache = fromCache,
                ErrorMessage = error != null && error.Length > 500 ? error[..500] : error,
                ClientHost = clientHost != null && clientHost.Length > 200 ? clientHost[..200] : clientHost
            }, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // A failed audit row must never break the user's request.
            _logger.LogError(ex, "Could not write the call log for {Source}", source.Key);
        }
    }

    private static ExecuteResponseDto BuildResponse(
        ApiSource source, CachedResult result, string host, bool fromCache, int durationMs)
    {
        return new ExecuteResponseDto
        {
            Key = source.Key,
            Title = source.Title,
            FetchedAtUtc = result.FetchedAtUtc,
            FromCache = fromCache,
            DurationMs = durationMs,
            UpstreamStatus = result.Status,
            UpstreamHost = host,
            Data = result.Data
        };
    }

    private static string BuildCacheKey(string key, IReadOnlyDictionary<string, string> values)
    {
        var parts = values.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => $"{v.Key}={v.Value}");
        return $"source:{key}:{string.Join("&", parts)}";
    }

    private static Dictionary<string, string> ParseHeaders(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    private static JsonElement ToJsonElement(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            // Not JSON (plain text answer): wrap it so the extension can still show it.
            return JsonSerializer.SerializeToElement(text);
        }
    }

    private sealed record CachedResult(JsonElement Data, int Status, DateTime FetchedAtUtc);
}
