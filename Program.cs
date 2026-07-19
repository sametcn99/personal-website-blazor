using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;
using personal_website_blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────────────
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

// ── Forwarded Headers ──────────────────────────────────────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Data Protection ────────────────────────────────────────────────────
var keysPath = builder.Configuration["DataProtection:KeysPath"];
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("personal-website-blazor");

if (!string.IsNullOrEmpty(keysPath))
{
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// ── Options ────────────────────────────────────────────────────────────
builder.Services
    .AddOptions<GitHubOptions>()
    .Bind(builder.Configuration.GetSection(GitHubOptions.SectionName));

builder.Services
    .AddOptions<CachePolicyOptions>()
    .Bind(builder.Configuration.GetSection(CachePolicyOptions.SectionName));

// ── Services ───────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISocialLinkProvider, SocialLinkProvider>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 512 * 1024;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddServerSideBlazor(options =>
{
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IRssFeedService, RssFeedService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<IGitHubService, GitHubService>();

builder.Services.AddHttpClient(
    "GitHub", client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "personal-website-blazor");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    });

// ── Build ──────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Exception Handling ─────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// ── Pipeline ───────────────────────────────────────────────────────────
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// ── Security Headers ───────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; "
        + "script-src 'self' 'unsafe-inline' cdnjs.cloudflare.com; "
        + "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com cdnjs.cloudflare.com; "
        + "img-src 'self' data: https:; "
        + "font-src 'self' https://fonts.gstatic.com data:; "
        + "connect-src 'self' wss: ws: https://umami.sametcc.me; "
        + "frame-ancestors 'none'; "
        + "base-uri 'self'; "
        + "form-action 'self'";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    await next();
});

// ── Rate Limiting (API) ────────────────────────────────────────────────
var rateLimitStore = new ConcurrentDictionary<string, RateLimitEntry>(StringComparer.OrdinalIgnoreCase);

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var entry = rateLimitStore.GetOrAdd(ip, _ => new RateLimitEntry());
    var windowStart = now / 60 * 60;

    lock (entry)
    {
        if (entry.WindowStart != windowStart)
        {
            entry.WindowStart = windowStart;
            entry.Count = 0;
        }

        entry.Count++;

        if (entry.Count > 120)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = "60";
            return;
        }
    }

    await next();
});

// ── Cache Policy ───────────────────────────────────────────────────────
var cacheOptions = app.Services.GetRequiredService<IOptions<CachePolicyOptions>>().Value;

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/_blazor"))
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
            return Task.CompletedTask;
        }

        if (path.Equals("/rss.xml", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/feed.json", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/manifest.webmanifest", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (context.Response.StatusCode == StatusCodes.Status200OK
            && Path.HasExtension(path))
        {
            var ext = Path.GetExtension(path.Value);
            if (ext is not null && IsStaticExtension(ext))
            {
                context.Response.Headers.CacheControl =
                    $"public, max-age={cacheOptions.StaticAssetsMaxAgeSeconds}";
                context.Response.Headers.Pragma = string.Empty;
                context.Response.Headers.Expires = string.Empty;
            }
        }

        return Task.CompletedTask;
    });

    await next();
});

// ── Antiforgery ────────────────────────────────────────────────────────
app.UseAntiforgery();

// ── Umami Analytics Proxy ──────────────────────────────────────────────
// Proxies the Umami tracking script through the same origin to avoid
// Edge's Tracking Prevention blocking localStorage access.
app.Map("/umami/script.js", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromSeconds(10);
    var response = await client.GetAsync("https://umami.sametcc.me/script.js");
    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = "application/javascript";
    context.Response.Headers.CacheControl = "public, max-age=3600";
    await response.Content.CopyToAsync(context.Response.Body);
});

// ── Controllers ────────────────────────────────────────────────────────
app.MapControllers();

// ── Razor Components ──────────────────────────────────────────────────
app.MapStaticAssets();
app.MapRazorComponents<personal_website_blazor.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────
static bool IsStaticExtension(string ext)
    => ext.Equals(".css", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".js", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".mjs", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".map", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".gif", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".svg", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".ico", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".woff", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".woff2", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".eot", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".otf", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
    || ext.Equals(".txt", StringComparison.OrdinalIgnoreCase);
