namespace NewsIntelligence.Infrastructure;

public sealed class WebzOptions
{
    public string Mode { get; set; } = "Mock";
    public string BaseUrl { get; set; } = "https://api.webz.io/";
    public string? ApiToken { get; set; }
}
