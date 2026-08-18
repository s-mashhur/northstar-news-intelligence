using Xunit;
using NewsIntelligence.Application;
using NewsIntelligence.Infrastructure;

namespace NewsIntelligence.Tests;

public sealed class CoreTests
{
    [Fact]
    public void Password_hash_round_trips_and_rejects_wrong_password()
    {
        var service = new PasswordService();
        var hash = service.Hash("Demo123!");
        Assert.True(service.Verify("Demo123!", hash));
        Assert.False(service.Verify("Wrong123!", hash));
        Assert.NotEqual("Demo123!", hash);
    }

    [Fact]
    public void Webz_query_builder_appends_language_filter()
    {
        var request = new NewsSearchRequest("OpenAI regulation", 7, "english", 20);
        Assert.Equal("OpenAI regulation language:english", WebzQueryBuilder.BuildQuery(request));
    }

    [Fact]
    public void Webz_timestamp_is_clamped_to_thirty_days()
    {
        var before = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();
        var value = WebzQueryBuilder.StartTimestampMilliseconds(999);
        var after = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds();
        Assert.InRange(value, before - 1000, after + 1000);
    }

    [Fact]
    public void Pagination_cursor_rejects_external_origins()
    {
        Assert.Throws<ArgumentException>(() => WebzQueryBuilder.NormalizeNextPath("https://evil.example/steal", "https://api.webz.io/"));
    }

    [Fact]
    public void Pagination_cursor_strips_token_and_keeps_webz_path()
    {
        var value = WebzQueryBuilder.NormalizeNextPath("https://api.webz.io/api/news?q=test&token=secret&size=10", "https://api.webz.io/");
        Assert.Equal("api/news?q=test&size=10", value);
        Assert.DoesNotContain("secret", value);
    }

    [Fact]
    public async Task Mock_provider_returns_requested_number_of_articles()
    {
        var provider = new MockNewsProvider();
        var result = await provider.SearchAsync(new NewsSearchRequest("NVIDIA", 7, "english", 12));
        Assert.Equal(12, result.Items.Count);
        Assert.All(result.Items, item => Assert.Contains("NVIDIA", item.Title, StringComparison.OrdinalIgnoreCase));
        Assert.True(result.TotalResults >= result.Items.Count);
    }
}
