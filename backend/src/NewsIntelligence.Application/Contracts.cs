namespace NewsIntelligence.Application;

public sealed record NewsSearchRequest(
    string Query,
    int Days = 7,
    string? Language = "english",
    int Size = 20,
    string? Next = null);

public sealed record NewsArticleDto(
    string Id,
    string Title,
    string Description,
    string Url,
    string? ImageUrl,
    string Source,
    string? Language,
    DateTimeOffset PublishedAt,
    string Sentiment,
    double? SentimentScore);

public sealed record NewsSearchResult(
    IReadOnlyList<NewsArticleDto> Items,
    long TotalResults,
    string? Next,
    string Provider);

public sealed record DashboardMetric(string Label, string Value, string Delta, string Tone);
public sealed record TimeSeriesPoint(string Label, int Value);
public sealed record SourceStat(string Name, int Count, double Percentage);
public sealed record SentimentStat(int Positive, int Neutral, int Negative);

public sealed record DashboardDto(
    IReadOnlyList<DashboardMetric> Metrics,
    IReadOnlyList<TimeSeriesPoint> Volume,
    IReadOnlyList<SourceStat> Sources,
    SentimentStat Sentiment,
    IReadOnlyList<NewsArticleDto> RecentArticles,
    string Provider);

public sealed record MonitorDto(
    Guid Id,
    string Name,
    string Query,
    string? Language,
    int Days,
    int IntervalMinutes,
    bool IsActive,
    DateTimeOffset? LastRunAt,
    int ArticleCount,
    DateTimeOffset CreatedAt);

public sealed record CreateMonitorRequest(
    string Name,
    string Query,
    string? Language = "english",
    int Days = 7,
    int IntervalMinutes = 30);

public sealed record AuthUser(Guid Id, string Email, string Role);
public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, AuthUser User);

public interface INewsProvider
{
    string Name { get; }
    Task<NewsSearchResult> SearchAsync(NewsSearchRequest request, CancellationToken cancellationToken = default);
}

public interface INewsService
{
    Task<NewsSearchResult> SearchAsync(Guid userId, NewsSearchRequest request, CancellationToken cancellationToken = default);
    Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IMonitorService
{
    Task<IReadOnlyList<MonitorDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MonitorDto> CreateAsync(Guid userId, CreateMonitorRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid userId, Guid monitorId, CancellationToken cancellationToken = default);
    Task<MonitorDto?> RunAsync(Guid userId, Guid monitorId, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<TokenPair> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<TokenPair?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<TokenPair?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
}

public interface ITokenIssuer
{
    (string Token, DateTimeOffset ExpiresAt) Issue(AuthUser user);
}
