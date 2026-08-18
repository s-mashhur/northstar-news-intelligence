using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NewsIntelligence.Api;
using NewsIntelligence.Application;
using NewsIntelligence.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ITokenIssuer, TokenIssuer>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("web", policy =>
{
    var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000", "http://localhost:8080"];
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    context.Response.ContentType = "application/problem+json";
    context.Response.StatusCode = exception is ArgumentException ? 400 : exception is InvalidOperationException ? 409 : 500;
    await Results.Problem(
        statusCode: context.Response.StatusCode,
        title: context.Response.StatusCode == 500 ? "Unexpected server error" : exception?.Message,
        detail: app.Environment.IsDevelopment() ? exception?.ToString() : null).ExecuteAsync(context);
}));

app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", version = "1.4.0", utc = DateTimeOffset.UtcNow })).AllowAnonymous();
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? (IResult)Results.Ok(new { status = "ready", database = "reachable" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();
app.MapOpenApi();

var api = app.MapGroup("/api").RequireRateLimiting("api");

var auth = api.MapGroup("/auth");
auth.MapPost("/register", async (RegisterRequest request, IAuthService service, CancellationToken ct) => Results.Ok(await service.RegisterAsync(request, ct)));
auth.MapPost("/login", async (LoginRequest request, IAuthService service, CancellationToken ct) =>
{
    var result = await service.LoginAsync(request, ct);
    return result is null ? (IResult)Results.Unauthorized() : Results.Ok(result);
});
auth.MapPost("/refresh", async (RefreshRequest request, IAuthService service, CancellationToken ct) =>
{
    var result = await service.RefreshAsync(request, ct);
    return result is null ? (IResult)Results.Unauthorized() : Results.Ok(result);
});

var secured = api.MapGroup("").RequireAuthorization();
secured.MapGet("/meta", (INewsProvider provider) => Results.Ok(new { provider = provider.Name, version = "1.4.0" }));
secured.MapPost("/news/search", async (NewsSearchRequest request, ClaimsPrincipal principal, INewsService service, CancellationToken ct) =>
    Results.Ok(await service.SearchAsync(UserId(principal), request, ct)));
secured.MapGet("/dashboard", async (ClaimsPrincipal principal, INewsService service, CancellationToken ct) =>
    Results.Ok(await service.GetDashboardAsync(UserId(principal), ct)));
secured.MapGet("/monitors", async (ClaimsPrincipal principal, IMonitorService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(UserId(principal), ct)));
secured.MapPost("/monitors", async (CreateMonitorRequest request, ClaimsPrincipal principal, IMonitorService service, CancellationToken ct) =>
    Results.Created("/api/monitors", await service.CreateAsync(UserId(principal), request, ct)));
secured.MapPost("/monitors/{id:guid}/run", async (Guid id, ClaimsPrincipal principal, IMonitorService service, CancellationToken ct) =>
{
    var result = await service.RunAsync(UserId(principal), id, ct);
    return result is null ? (IResult)Results.NotFound() : Results.Ok(result);
});
secured.MapDelete("/monitors/{id:guid}", async (Guid id, ClaimsPrincipal principal, IMonitorService service, CancellationToken ct) =>
    await service.DeleteAsync(UserId(principal), id, ct) ? (IResult)Results.NoContent() : Results.NotFound());

await DatabaseBootstrapper.InitializeAsync(app.Services);
app.Run();

static Guid UserId(ClaimsPrincipal principal)
{
    var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Missing user id claim.");
    return Guid.Parse(raw);
}

public partial class Program { }
