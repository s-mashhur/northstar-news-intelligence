using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewsIntelligence.Application;

namespace NewsIntelligence.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["Database:Provider"] ?? "InMemory";
        services.AddDbContext<AppDbContext>(options =>
        {
            if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(
                    configuration.GetConnectionString("Postgres"),
                    npgsql => npgsql.EnableRetryOnFailure(
                        maxRetryCount: 8,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null));
            else
                options.UseInMemoryDatabase("news-intelligence-dev");
        });

        services.Configure<WebzOptions>(configuration.GetSection("Webz"));
        services.Configure<SeedOptions>(configuration.GetSection("Seed"));

        services.AddHttpClient<WebzNewsProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WebzOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NewsIntelligencePortfolio/1.0");
        });
        services.AddSingleton<MockNewsProvider>();
        services.AddScoped<INewsProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebzOptions>>().Value;
            return options.Mode.Equals("Real", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<WebzNewsProvider>()
                : sp.GetRequiredService<MockNewsProvider>();
        });

        var redisEnabled = configuration.GetValue("Redis:Enabled", false);
        if (redisEnabled)
            services.AddStackExchangeRedisCache(o => o.Configuration = configuration["Redis:ConnectionString"] ?? "localhost:6379");
        else
            services.AddDistributedMemoryCache();

        services.AddSingleton<PasswordService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IMonitorService, MonitorService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddHostedService<MonitorWorker>();
        return services;
    }
}
