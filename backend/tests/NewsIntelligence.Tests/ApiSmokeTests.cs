using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NewsIntelligence.Application;
using Xunit;

namespace NewsIntelligence.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Redis:Enabled", "false");
        builder.UseSetting("Webz:Mode", "Mock");
        builder.UseSetting("Seed:Enabled", "true");
        builder.UseSetting("Jwt:Secret", "integration-test-secret-that-is-longer-than-32-characters");
    }
}

public sealed class ApiSmokeTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_is_available()
    {
        using var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_demo_user_can_login()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("demo@newsintel.dev", "Demo123!"));
        response.EnsureSuccessStatusCode();
        var pair = await response.Content.ReadFromJsonAsync<TokenPair>();
        Assert.NotNull(pair);
        Assert.False(string.IsNullOrWhiteSpace(pair.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(pair.RefreshToken));
    }
}
