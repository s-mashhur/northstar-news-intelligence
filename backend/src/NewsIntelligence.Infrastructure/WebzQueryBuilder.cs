using NewsIntelligence.Application;

namespace NewsIntelligence.Infrastructure;

public static class WebzQueryBuilder
{
    public static string BuildQuery(NewsSearchRequest request)
    {
        var query = request.Query.Trim();
        if (!string.IsNullOrWhiteSpace(request.Language) && !request.Language.Equals("all", StringComparison.OrdinalIgnoreCase))
            query += $" language:{request.Language.Trim().ToLowerInvariant()}";
        return query;
    }

    public static long StartTimestampMilliseconds(int days)
    {
        var safeDays = Math.Clamp(days, 1, 30);
        return DateTimeOffset.UtcNow.AddDays(-safeDays).ToUnixTimeMilliseconds();
    }

    public static string CacheKey(NewsSearchRequest request) =>
        $"news:{request.Query.Trim().ToLowerInvariant()}:{request.Language?.Trim().ToLowerInvariant()}:{Math.Clamp(request.Days, 1, 30)}:{Math.Clamp(request.Size, 1, 100)}:{request.Next}";

    public static string NormalizeNextPath(string next, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(next)) throw new ArgumentException("Pagination cursor is empty.");
        var baseUri = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute);
        if (!Uri.TryCreate(baseUri, next, out var resolved)) throw new ArgumentException("Invalid pagination cursor.");

        var sameOrigin = resolved.Scheme.Equals(baseUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && resolved.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase)
            && resolved.Port == baseUri.Port;
        if (!sameOrigin) throw new ArgumentException("Pagination cursor must target the configured Webz.io API origin.");

        var path = resolved.AbsolutePath.TrimStart('/');
        if (!path.StartsWith("api/news", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Pagination cursor must target the Webz.io News endpoint.");

        var safeParts = resolved.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.StartsWith("token=", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return safeParts.Length == 0 ? path : $"{path}?{string.Join('&', safeParts)}";
    }

    public static string? TryNormalizeNextPath(string? next, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(next)) return null;
        try { return NormalizeNextPath(next, baseUrl); }
        catch (ArgumentException) { return null; }
    }
}
