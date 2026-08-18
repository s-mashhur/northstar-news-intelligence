namespace NewsIntelligence.Domain;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = "User";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<NewsMonitor> Monitors { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public User? User { get; set; }
}

public sealed class NewsMonitor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string Query { get; set; }
    public string? Language { get; set; } = "english";
    public int Days { get; set; } = 7;
    public int IntervalMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public User? User { get; set; }
    public List<Article> Articles { get; set; } = [];
}

public sealed class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MonitorId { get; set; }
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Url { get; set; }
    public string? ImageUrl { get; set; }
    public required string Source { get; set; }
    public string? Language { get; set; }
    public string Sentiment { get; set; } = "neutral";
    public double? SentimentScore { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public NewsMonitor? Monitor { get; set; }
}
