using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NewsIntelligence.Application;
using NewsIntelligence.Domain;

namespace NewsIntelligence.Infrastructure;

public sealed class NewsService(AppDbContext db, INewsProvider provider, IDistributedCache cache, ILogger<NewsService> logger) : INewsService
{
    public async Task<NewsSearchResult> SearchAsync(Guid userId, NewsSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) throw new ArgumentException("Query is required.");
        var normalized = request with { Days = Math.Clamp(request.Days, 1, 30), Size = Math.Clamp(request.Size, 1, 100) };
        var key = WebzQueryBuilder.CacheKey(normalized);
        try
        {
            var cached = await cache.GetStringAsync(key, cancellationToken);
            if (cached is not null)
            {
                var value = JsonSerializer.Deserialize<NewsSearchResult>(cached);
                if (value is not null) return value;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Search cache read failed; continuing without cache.");
        }

        var result = await provider.SearchAsync(normalized, cancellationToken);
        try
        {
            await cache.SetStringAsync(key, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Search cache write failed; returning uncached provider result.");
        }
        return result;
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var articles = await db.Articles.AsNoTracking()
            .Where(x => x.Monitor!.UserId == userId)
            .OrderByDescending(x => x.PublishedAt)
            .Take(5000)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var last24 = articles.Count(x => x.PublishedAt >= now.AddHours(-24));
        var previous24 = articles.Count(x => x.PublishedAt >= now.AddHours(-48) && x.PublishedAt < now.AddHours(-24));
        var delta = previous24 == 0 ? (last24 > 0 ? 100 : 0) : (int)Math.Round((last24 - previous24) * 100.0 / previous24);
        var activeMonitors = await db.Monitors.CountAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
        var positive = articles.Count(x => x.Sentiment == "positive");
        var negative = articles.Count(x => x.Sentiment == "negative");
        var neutral = Math.Max(0, articles.Count - positive - negative);
        var positivePct = articles.Count == 0 ? 0 : (int)Math.Round(positive * 100.0 / articles.Count);

        var metrics = new List<DashboardMetric>
        {
            new("Tracked articles", articles.Count.ToString("N0"), "+ portfolio dataset", "violet"),
            new("Last 24 hours", last24.ToString("N0"), $"{(delta >= 0 ? "+" : "")}{delta}% vs prior day", delta >= 0 ? "green" : "red"),
            new("Active monitors", activeMonitors.ToString(), "scheduled intelligence", "cyan"),
            new("Positive coverage", $"{positivePct}%", "of tracked coverage", "green")
        };

        var volume = Enumerable.Range(0, 14).Select(offset =>
        {
            var day = new DateTimeOffset(now.UtcDateTime.Date.AddDays(-13 + offset), TimeSpan.Zero);
            var next = day.AddDays(1);
            return new TimeSeriesPoint(day.ToString("MMM dd"), articles.Count(x => x.PublishedAt >= day && x.PublishedAt < next));
        }).ToList();

        var sourceGroups = articles.GroupBy(x => x.Source).OrderByDescending(g => g.Count()).Take(6).ToList();
        var sourceTotal = Math.Max(1, sourceGroups.Sum(g => g.Count()));
        var sources = sourceGroups.Select(g => new SourceStat(g.Key, g.Count(), Math.Round(g.Count() * 100.0 / sourceTotal, 1))).ToList();
        var recent = articles.Take(8).Select(ToDto).ToList();

        return new DashboardDto(metrics, volume, sources, new SentimentStat(positive, neutral, negative), recent, provider.Name);
    }

    private static NewsArticleDto ToDto(Article a) => new(a.ExternalId, a.Title, a.Description ?? "", a.Url, a.ImageUrl, a.Source, a.Language, a.PublishedAt, a.Sentiment, a.SentimentScore);
}

public sealed class MonitorService(AppDbContext db, INewsProvider provider) : IMonitorService
{
    public async Task<IReadOnlyList<MonitorDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.Monitors.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt)
            .Select(x => new MonitorDto(x.Id, x.Name, x.Query, x.Language, x.Days, x.IntervalMinutes, x.IsActive, x.LastRunAt, x.Articles.Count, x.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<MonitorDto> CreateAsync(Guid userId, CreateMonitorRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Query)) throw new ArgumentException("Name and query are required.");
        var monitor = new NewsMonitor
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Query = request.Query.Trim(),
            Language = string.IsNullOrWhiteSpace(request.Language) ? "english" : request.Language.Trim(),
            Days = Math.Clamp(request.Days, 1, 30),
            IntervalMinutes = Math.Clamp(request.IntervalMinutes, 15, 1440)
        };
        db.Monitors.Add(monitor);
        await db.SaveChangesAsync(cancellationToken);
        await ExecuteMonitorAsync(monitor, cancellationToken);
        return await GetDtoAsync(userId, monitor.Id, cancellationToken) ?? throw new InvalidOperationException("Monitor creation failed.");
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid monitorId, CancellationToken cancellationToken = default)
    {
        var monitor = await db.Monitors.FirstOrDefaultAsync(x => x.Id == monitorId && x.UserId == userId, cancellationToken);
        if (monitor is null) return false;
        db.Monitors.Remove(monitor);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MonitorDto?> RunAsync(Guid userId, Guid monitorId, CancellationToken cancellationToken = default)
    {
        var monitor = await db.Monitors.FirstOrDefaultAsync(x => x.Id == monitorId && x.UserId == userId, cancellationToken);
        if (monitor is null) return null;
        await ExecuteMonitorAsync(monitor, cancellationToken);
        return await GetDtoAsync(userId, monitorId, cancellationToken);
    }

    public async Task ExecuteMonitorAsync(NewsMonitor monitor, CancellationToken cancellationToken)
    {
        var result = await provider.SearchAsync(new NewsSearchRequest(monitor.Query, monitor.Days, monitor.Language, 40), cancellationToken);
        var existing = await db.Articles.Where(x => x.MonitorId == monitor.Id).Select(x => x.ExternalId).ToHashSetAsync(cancellationToken);
        foreach (var article in result.Items.Where(x => !existing.Contains(x.Id)))
        {
            db.Articles.Add(new Article
            {
                MonitorId = monitor.Id,
                ExternalId = article.Id,
                Title = article.Title,
                Description = article.Description,
                Url = article.Url,
                ImageUrl = article.ImageUrl,
                Source = article.Source,
                Language = article.Language,
                PublishedAt = article.PublishedAt,
                Sentiment = article.Sentiment,
                SentimentScore = article.SentimentScore
            });
        }
        monitor.LastRunAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MonitorDto?> GetDtoAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.Monitors.AsNoTracking().Where(x => x.UserId == userId && x.Id == id)
            .Select(x => new MonitorDto(x.Id, x.Name, x.Query, x.Language, x.Days, x.IntervalMinutes, x.IsActive, x.LastRunAt, x.Articles.Count, x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
}

public sealed class PasswordService
{
    private const int Iterations = 600_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
    }
}

public sealed class AuthService(AppDbContext db, PasswordService passwords, ITokenIssuer tokenIssuer) : IAuthService
{
    public async Task<TokenPair> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) throw new InvalidOperationException("An account with this email already exists.");
        var user = new User { Email = email, PasswordHash = passwords.Hash(request.Password) };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return await CreatePairAsync(user, cancellationToken);
    }

    public async Task<TokenPair?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash)) return null;
        return await CreatePairAsync(user, cancellationToken);
    }

    public async Task<TokenPair?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored?.User is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTimeOffset.UtcNow) return null;
        stored.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await CreatePairAsync(stored.User, cancellationToken);
    }

    private async Task<TokenPair> CreatePairAsync(User user, CancellationToken cancellationToken)
    {
        var authUser = new AuthUser(user.Id, user.Email, user.Role);
        var access = tokenIssuer.Issue(authUser);
        var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14)
        });
        await db.SaveChangesAsync(cancellationToken);
        return new TokenPair(access.Token, rawRefresh, access.ExpiresAt, authUser);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (!System.Net.Mail.MailAddress.TryCreate(normalized, out _))
            throw new ArgumentException("A valid email address is required.");
        return normalized;
    }
    private static void ValidatePassword(string password)
    {
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new ArgumentException("Password must be at least 8 characters and contain upper-case, lower-case and a number.");
    }
}
