using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NewsIntelligence.Application;

namespace NewsIntelligence.Infrastructure;

public sealed class WebzNewsProvider(HttpClient httpClient, IOptions<WebzOptions> options) : INewsProvider
{
    private readonly WebzOptions _options = options.Value;
    public string Name => "Webz.io";

    public async Task<NewsSearchResult> SearchAsync(NewsSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
            throw new InvalidOperationException("Webz real mode is enabled, but Webz:ApiToken is empty.");

        var size = Math.Clamp(request.Size, 1, 100);
        var query = WebzQueryBuilder.BuildQuery(request);
        var ts = WebzQueryBuilder.StartTimestampMilliseconds(request.Days);

        var path = string.IsNullOrWhiteSpace(request.Next)
            ? $"api/news?q={Uri.EscapeDataString(query)}&ts={ts}&size={size}"
            : WebzQueryBuilder.NormalizeNextPath(request.Next!, _options.BaseUrl);

        using var message = new HttpRequestMessage(HttpMethod.Get, path);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Webz.io returned {(int)response.StatusCode}: {body[..Math.Min(body.Length, 500)]}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var total = ReadLong(root, "total_results", "totalResults");
        var next = WebzQueryBuilder.TryNormalizeNextPath(ReadString(root, "next"), _options.BaseUrl);
        var items = new List<NewsArticleDto>();

        if (TryGetArray(root, out var array, "posts", "articles", "results"))
        {
            foreach (var item in array.EnumerateArray())
            {
                var id = ReadString(item, "uuid", "id") ?? Guid.NewGuid().ToString("N");
                var title = ReadString(item, "title") ?? "Untitled article";
                var description = ReadString(item, "text", "description", "summary") ?? string.Empty;
                var url = SafeHttpUrl(ReadString(item, "url") ?? ReadNestedString(item, "thread", "url")) ?? "#";
                var image = SafeHttpUrl(ReadString(item, "image", "image_url") ?? ReadNestedString(item, "thread", "main_image"));
                var source = ReadNestedString(item, "thread", "site") ?? ReadString(item, "source", "site") ?? "Unknown source";
                var language = ReadString(item, "language");
                var published = ReadDate(item, "published", "published_at", "publishedAt") ?? DateTimeOffset.UtcNow;
                var sentiment = ReadString(item, "sentiment") ?? ReadNestedString(item, "sentiment", "label") ?? "neutral";
                var score = ReadDouble(item, "sentiment_score") ?? ReadNestedDouble(item, "sentiment", "score");
                items.Add(new NewsArticleDto(id, title, description, url, image, source, language, published, sentiment.ToLowerInvariant(), score));
            }
        }

        return new NewsSearchResult(items, total == 0 ? items.Count : total, next, Name);
    }

    private static bool TryGetArray(JsonElement root, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Array) return true;
        value = default;
        return false;
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }

    private static string? ReadNestedString(JsonElement element, string parent, string child)
    {
        if (element.TryGetProperty(parent, out var nested) && nested.ValueKind == JsonValueKind.Object)
            return ReadString(nested, child);
        return null;
    }

    private static long ReadLong(JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result)) return result;
        return 0;
    }

    private static double? ReadDouble(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.TryGetDouble(out var result)) return result;
        return null;
    }

    private static double? ReadNestedDouble(JsonElement element, string parent, string child)
    {
        if (element.TryGetProperty(parent, out var nested) && nested.ValueKind == JsonValueKind.Object)
            return ReadDouble(nested, child);
        return null;
    }

    private static DateTimeOffset? ReadDate(JsonElement element, params string[] names)
    {
        var raw = ReadString(element, names);
        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static string? SafeHttpUrl(string? raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return null;
        return uri.Scheme is "http" or "https" ? uri.ToString() : null;
    }

}

public sealed class MockNewsProvider : INewsProvider
{
    public string Name => "Mock demo data";

    private static readonly string[] Sources = ["Reuters", "Bloomberg", "TechCrunch", "Financial Times", "The Verge", "CNBC", "Associated Press", "Wired"];
    private static readonly string[] Themes = [
        "accelerates its AI infrastructure roadmap",
        "announces a strategic enterprise partnership",
        "faces fresh regulatory scrutiny in Europe",
        "reports stronger-than-expected market momentum",
        "expands cloud and data-center investments",
        "launches a new developer platform initiative",
        "draws investor attention after product update",
        "publishes new sustainability and governance targets",
        "reshapes its competitive strategy for 2026",
        "sees renewed demand across enterprise customers",
        "unveils a research collaboration focused on safety",
        "adds new capabilities to its core product suite"
    ];

    public Task<NewsSearchResult> SearchAsync(NewsSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrWhiteSpace(request.Query) ? "technology" : request.Query.Trim();
        var seed = StablePositiveInt(query);
        var total = 640 + seed % 1700;
        var count = Math.Clamp(request.Size, 1, 40);
        var now = DateTimeOffset.UtcNow;
        var list = new List<NewsArticleDto>(count);

        for (var i = 0; i < count; i++)
        {
            var source = Sources[(seed + i * 3) % Sources.Length];
            var theme = Themes[(seed + i) % Themes.Length];
            var sentimentScore = (((seed / 7 + i * 17) % 181) - 90) / 100.0;
            var sentiment = sentimentScore > .22 ? "positive" : sentimentScore < -.22 ? "negative" : "neutral";
            var published = now.AddMinutes(-(i * 71 + seed % 47));
            var id = $"mock-{StableId($"{query.ToLowerInvariant()}|{i}|{published:yyyy-MM-dd}")}";
            list.Add(new NewsArticleDto(
                id,
                $"{query} {theme}",
                $"A demo article showing how {query} coverage can be searched, monitored and analyzed. Replace Mock mode with a Webz.io token for live news data.",
                $"https://example.com/news/{Uri.EscapeDataString(id)}",
                null,
                source,
                request.Language is null or "all" ? "english" : request.Language,
                published,
                sentiment,
                Math.Round(sentimentScore, 2)));
        }

        return Task.FromResult(new NewsSearchResult(list, total, null, Name));
    }
    private static int StablePositiveInt(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
        return BitConverter.ToInt32(bytes, 0) & int.MaxValue;
    }

    private static string StableId(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
}

