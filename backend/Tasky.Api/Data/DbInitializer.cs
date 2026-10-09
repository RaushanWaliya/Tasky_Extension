using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tasky.Api.Domain.Entities;

namespace Tasky.Api.Data;

/// <summary>
/// SQL Server: nothing to do, run database/schema.sql yourself.
/// InMemory demo mode (Database:Provider = InMemory): creates the store and loads the starter APIs
/// from Data/sources.seed.json so you can try the project without installing SQL Server.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var provider = config["Database:Provider"] ?? "SqlServer";
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskyDbContext>();

        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync();
            if (!await db.ApiSources.AnyAsync())
            {
                var seedFile = Path.Combine(AppContext.BaseDirectory, config["Database:SeedFile"] ?? "Data/sources.seed.json");
                await SeedFromFileAsync(db, seedFile);
                logger.LogInformation("InMemory database seeded with {Count} API sources.", await db.ApiSources.CountAsync());
            }
            return;
        }

        try
        {
            if (!await db.Database.CanConnectAsync())
            {
                logger.LogWarning("Cannot connect to SQL Server. Check ConnectionStrings:TaskyDb and run database/schema.sql.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SQL Server check failed. Check ConnectionStrings:TaskyDb and run database/schema.sql.");
        }
    }

    private static async Task SeedFromFileAsync(TaskyDbContext db, string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var items = JsonSerializer.Deserialize<List<SeedItem>>(json, options) ?? new List<SeedItem>();

        var order = 1;
        foreach (var item in items)
        {
            db.ApiSources.Add(new ApiSource
            {
                Key = item.Key,
                Title = item.Title,
                Icon = item.Icon,
                Description = item.Description,
                SortOrder = order++,
                HttpMethod = item.HttpMethod,
                UrlTemplate = item.UrlTemplate,
                BodyTemplate = item.BodyTemplate,
                HeadersJson = item.Headers.GetRawText(),
                InputSchemaJson = item.Inputs.GetRawText(),
                DisplayType = item.DisplayType,
                DisplayConfigJson = item.DisplayConfig.GetRawText(),
                CacheSeconds = item.CacheSeconds,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed class SeedItem
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "GET";
        public string UrlTemplate { get; set; } = string.Empty;
        public string? BodyTemplate { get; set; }
        public JsonElement Headers { get; set; }
        public JsonElement Inputs { get; set; }
        public string DisplayType { get; set; } = "list";
        public JsonElement DisplayConfig { get; set; }
        public int CacheSeconds { get; set; }
    }
}
