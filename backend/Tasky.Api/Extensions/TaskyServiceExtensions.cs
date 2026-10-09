using Microsoft.EntityFrameworkCore;
using Tasky.Api.Data;
using Tasky.Api.Options;
using Tasky.Api.Repositories;
using Tasky.Api.Services;

namespace Tasky.Api.Extensions;

/// <summary>Keeps Program.cs short: each method registers one area of the application.</summary>
public static class TaskyServiceExtensions
{
    public const string CorsPolicyName = "TaskyExtension";

    public static IServiceCollection AddTaskyDatabase(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "SqlServer";

        services.AddDbContext<TaskyDbContext>(options =>
        {
            if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("TaskyDemoDb");
            }
            else
            {
                options.UseSqlServer(config.GetConnectionString("TaskyDb"));
            }
        });

        return services;
    }

    public static IServiceCollection AddTaskyServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<UpstreamOptions>(config.GetSection(UpstreamOptions.SectionName));
        var upstream = config.GetSection(UpstreamOptions.SectionName).Get<UpstreamOptions>() ?? new UpstreamOptions();

        // One shared HttpClient setup for every external API.
        services.AddHttpClient("upstream", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(upstream.TimeoutSeconds);
            client.MaxResponseContentBufferSize = upstream.MaxResponseBytes;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(upstream.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddMemoryCache();

        services.AddScoped<IApiSourceRepository, ApiSourceRepository>();
        services.AddScoped<IApiCallLogRepository, ApiCallLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ISourceCatalogService, SourceCatalogService>();
        services.AddScoped<IProxyService, ProxyService>();

        return services;
    }

    public static IServiceCollection AddTaskyCors(this IServiceCollection services, IConfiguration config)
    {
        var extraOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                // Chrome extensions call from chrome-extension://<id>. The id is different on every PC,
                // so any extension origin is accepted. Add website origins in appsettings if you need them.
                policy
                    .SetIsOriginAllowed(origin =>
                        origin.StartsWith("chrome-extension://", StringComparison.Ordinal)
                        || extraOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
