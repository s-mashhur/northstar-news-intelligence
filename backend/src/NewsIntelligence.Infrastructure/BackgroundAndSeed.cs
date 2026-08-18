using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsIntelligence.Application;
using NewsIntelligence.Domain;

namespace NewsIntelligence.Infrastructure;

public sealed class MonitorWorker(IServiceScopeFactory scopeFactory, ILogger<MonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<IMonitorService>() as MonitorService;
                if (service is null) continue;
                var now = DateTimeOffset.UtcNow;
                var candidates = await db.Monitors.Where(x => x.IsActive)
                    .OrderBy(x => x.LastRunAt)
                    .Take(100)
                    .ToListAsync(stoppingToken);
                var due = candidates
                    .Where(x => x.LastRunAt is null || x.LastRunAt.Value.AddMinutes(x.IntervalMinutes) <= now)
                    .Take(20);
                foreach (var monitor in due)
                {
                    try { await service.ExecuteMonitorAsync(monitor, stoppingToken); }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex) { logger.LogError(ex, "Monitor {MonitorId} execution failed.", monitor.Id); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Scheduled monitor execution failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

public sealed class SeedOptions
{
    public bool Enabled { get; set; }
    public string Email { get; set; } = "demo@newsintel.dev";
    public string Password { get; set; } = "Demo123!";
}

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseBootstrapper");

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (db.Database.IsRelational())
                    await db.Database.MigrateAsync(cancellationToken);
                else
                    await db.Database.EnsureCreatedAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(2 * attempt, 10));
                logger.LogWarning(ex, "Database initialization attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds}s.", attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        var seed = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (!seed.Enabled) return;

        var email = seed.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
            user = new User { Email = email, PasswordHash = passwords.Hash(seed.Password) };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Monitors.AnyAsync(x => x.UserId == user.Id, cancellationToken))
        {
            db.Monitors.AddRange(
                new NewsMonitor { UserId = user.Id, Name = "OpenAI Intelligence", Query = "OpenAI artificial intelligence", Language = "english", Days = 7, IntervalMinutes = 30 },
                new NewsMonitor { UserId = user.Id, Name = "NVIDIA Market Watch", Query = "NVIDIA GPU AI", Language = "english", Days = 7, IntervalMinutes = 60 },
                new NewsMonitor { UserId = user.Id, Name = "Microsoft AI", Query = "Microsoft Azure AI", Language = "english", Days = 14, IntervalMinutes = 120 });
            await db.SaveChangesAsync(cancellationToken);
        }

        var monitorService = scope.ServiceProvider.GetRequiredService<IMonitorService>() as MonitorService;
        if (monitorService is not null && !await db.Articles.AnyAsync(x => x.Monitor!.UserId == user.Id, cancellationToken))
        {
            var monitors = await db.Monitors.Where(x => x.UserId == user.Id).ToListAsync(cancellationToken);
            foreach (var monitor in monitors) await monitorService.ExecuteMonitorAsync(monitor, cancellationToken);
        }
    }
}
