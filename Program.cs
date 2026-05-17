using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using personal_website_blazor.Endpoints;
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
builder.Services.AddDataProtection().SetApplicationName("personal-website-blazor");

// ── Options ────────────────────────────────────────────────────────────
builder.Services
    .AddOptions<GitHubOptions>()
    .Bind(builder.Configuration.GetSection(GitHubOptions.SectionName));

builder.Services
    .AddOptions<CachePolicyOptions>()
    .Bind(builder.Configuration.GetSection(CachePolicyOptions.SectionName));

// ── Services ───────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 4 * 1024 * 1024;
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

// ── Pipeline ───────────────────────────────────────────────────────────
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

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

// ── Razor Components ──────────────────────────────────────────────────
app.MapStaticAssets();
app.MapRazorComponents<personal_website_blazor.Components.App>()
    .AddInteractiveServerRenderMode();

// ── API Endpoints ─────────────────────────────────────────────────────
app.MapContentApiEndpoints();
app.MapSyndicationEndpoints();
app.MapMetadataEndpoints();

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
