using Microsoft.EntityFrameworkCore;
using Tasky.Api.Domain.Entities;

namespace Tasky.Api.Data;

public class TaskyDbContext : DbContext
{
    public TaskyDbContext(DbContextOptions<TaskyDbContext> options) : base(options)
    {
    }

    public DbSet<ApiSource> ApiSources => Set<ApiSource>();
    public DbSet<ApiCallLog> ApiCallLogs => Set<ApiCallLog>();

    /// <summary>
    /// Only maps C# names to the SQL names created by database/schema.sql.
    /// It does NOT create tables (no migrations in this project).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiSource>(e =>
        {
            e.ToTable("api_sources");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Key).HasColumnName("source_key").HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(100).IsRequired();
            e.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(10).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(300).IsRequired();
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.HttpMethod).HasColumnName("http_method").HasMaxLength(10).IsRequired();
            e.Property(x => x.UrlTemplate).HasColumnName("url_template").HasMaxLength(1000).IsRequired();
            e.Property(x => x.BodyTemplate).HasColumnName("body_template");
            e.Property(x => x.HeadersJson).HasColumnName("headers_json");
            e.Property(x => x.InputSchemaJson).HasColumnName("input_schema_json");
            e.Property(x => x.DisplayType).HasColumnName("display_type").HasMaxLength(20).IsRequired();
            e.Property(x => x.DisplayConfigJson).HasColumnName("display_config_json");
            e.Property(x => x.CacheSeconds).HasColumnName("cache_seconds");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<ApiCallLog>(e =>
        {
            e.ToTable("api_call_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ApiSourceId).HasColumnName("api_source_id");
            e.Property(x => x.RequestedAtUtc).HasColumnName("requested_at_utc");
            e.Property(x => x.DurationMs).HasColumnName("duration_ms");
            e.Property(x => x.StatusCode).HasColumnName("status_code");
            e.Property(x => x.IsSuccess).HasColumnName("is_success");
            e.Property(x => x.FromCache).HasColumnName("from_cache");
            e.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(500);
            e.Property(x => x.ClientHost).HasColumnName("client_host").HasMaxLength(200);
            e.HasOne(x => x.ApiSource).WithMany().HasForeignKey(x => x.ApiSourceId);
        });
    }
}
