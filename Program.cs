using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using personal_website_blazor.Components;
using personal_website_blazor.Core.Application.Abstractions;
using personal_website_blazor.Core.Application.Configuration;
using personal_website_blazor.Core.Domain.Entities;
using personal_website_blazor.Infrastructure.Content;
using personal_website_blazor.Infrastructure.Feeds;
using personal_website_blazor.Infrastructure.GitHub;
using personal_website_blazor.Infrastructure.Seo;

var builder = WebApplication.CreateBuilder(args);

// Disable reload on change to prevent inotify issues in Docker/Linux environments
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: false
);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;

    // This app runs behind container/reverse-proxy setups with dynamic upstream IPs.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder
    .Services.AddOptions<GitHubOptions>()
    .Bind(builder.Configuration.GetSection(GitHubOptions.SectionName));

builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IRssFeedService, RssFeedService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<IGitHubService, GitHubService>();

builder.Services.AddHttpClient(
    "GitHub",
    client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "personal-website-blazor");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }
);

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.Use(
    async (context, next) =>
    {
        await next();

        if (context.Request.Path.StartsWithSegments("/_blazor"))
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
        }
    }
);

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapGet(
    "/rss.xml",
    async (HttpContext context, IRssFeedService feedService) =>
    {
        var request = context.Request;
        var baseUri = new Uri($"{request.Scheme}://{request.Host}");

        var xml = await feedService.BuildFeedAsync(baseUri);

        context.Response.Headers.CacheControl = "public, max-age=1800";
        return Results.Content(xml, "application/rss+xml");
    }
);

app.MapGet(
    "/sitemap.xml",
    async (HttpContext context, ISitemapService sitemapService) =>
    {
        var request = context.Request;
        var baseUri = new Uri($"{request.Scheme}://{request.Host}");

        var xml = await sitemapService.GenerateSitemapXmlAsync(baseUri);

        context.Response.Headers.CacheControl = "public, max-age=86400";
        return Results.Content(xml, "application/xml");
    }
);

app.MapGet(
    "/manifest.webmanifest",
    (HttpContext context) =>
    {
        var manifest = new
        {
            name = "Samet Can Cıncık | Web Developer",
            short_name = "Samet Can",
            description = "Web Developer passionate about creating compelling and user-friendly web experiences.",
            start_url = "/",
            display = "standalone",
            background_color = "#0c0c0cff",
            theme_color = "#0c0c0cff",
            orientation = "portrait-primary",
            scope = "/",
            lang = "en",
            categories = new[] { "education", "productivity", "developer" },
            icons = new[]
            {
                new
                {
                    src = "/favicon-16x16.png",
                    sizes = "16x16",
                    type = "image/png",
                    purpose = "any",
                },
                new
                {
                    src = "/favicon-32x32.png",
                    sizes = "32x32",
                    type = "image/png",
                    purpose = "any",
                },
                new
                {
                    src = "/android-chrome-192x192.png",
                    sizes = "192x192",
                    type = "image/png",
                    purpose = "any",
                },
                new
                {
                    src = "/android-chrome-512x512.png",
                    sizes = "512x512",
                    type = "image/png",
                    purpose = "any",
                },
                new
                {
                    src = "/apple-touch-icon.png",
                    sizes = "180x180",
                    type = "image/png",
                    purpose = "any",
                },
            },
            shortcuts = new object[]
            {
                new
                {
                    name = "Blog",
                    short_name = "Blog",
                    description = "View blog posts",
                    url = "/blog",
                    icons = new[]
                    {
                        new
                        {
                            src = "/android-chrome-192x192.png",
                            sizes = "192x192",
                            type = "image/png",
                        },
                    },
                },
                new
                {
                    name = "Gists",
                    short_name = "Gists",
                    description = "View coding gists and tutorials",
                    url = "/gist",
                    icons = new[]
                    {
                        new
                        {
                            src = "/android-chrome-192x192.png",
                            sizes = "192x192",
                            type = "image/png",
                        },
                    },
                },
                new
                {
                    name = "CV",
                    short_name = "CV",
                    description = "View curriculum vitae",
                    url = "/cv",
                    icons = new[]
                    {
                        new
                        {
                            src = "/android-chrome-192x192.png",
                            sizes = "192x192",
                            type = "image/png",
                        },
                    },
                },
            },
            prefer_related_applications = false,
            related_applications = Array.Empty<object>(),
            dir = "ltr",
        };

        context.Response.Headers.CacheControl = "public, max-age=600, must-revalidate";
        var json = JsonSerializer.Serialize(manifest);
        return Results.Content(json, "application/manifest+json");
    }
);

app.MapGet(
    "/opengraph-image",
    () =>
    {
        const string svg = """
<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
    <defs>
        <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stop-color="#0c0c0c" />
            <stop offset="100%" stop-color="#151515" />
        </linearGradient>
    </defs>
    <rect width="1200" height="630" fill="url(#bg)" />
    <text x="80" y="270" fill="#ffffff" font-size="64" font-family="Arial, Helvetica, sans-serif" font-weight="700">Samet Can Cıncık</text>
    <text x="80" y="340" fill="#90caf9" font-size="40" font-family="Arial, Helvetica, sans-serif">Web Developer</text>
    <text x="80" y="410" fill="#b0b0b0" font-size="28" font-family="Arial, Helvetica, sans-serif">sametcc.me</text>
</svg>
""";

        return Results.Content(svg, "image/svg+xml");
    }
);

// Content API endpoints
app.MapGet(
    "/api/blog",
    async (IContentService contentService, string? search, string? tag, int? page, int? limit) =>
    {
        var posts = await contentService.GetPostsAsync("posts");
        return FilterAndPaginate(posts, search, tag, page, limit, "blog");
    }
);

app.MapGet(
    "/api/gists",
    async (IContentService contentService, string? search, string? tag, int? page, int? limit) =>
    {
        var posts = await contentService.GetPostsAsync("gists");
        return FilterAndPaginate(posts, search, tag, page, limit, "gist");
    }
);

app.MapGet(
    "/api/projects",
    async (IContentService contentService, string? search, string? tag, int? page, int? limit) =>
    {
        var posts = await contentService.GetPostsAsync("projects");
        return FilterAndPaginate(posts, search, tag, page, limit, "project");
    }
);

app.MapGet(
    "/api/repos",
    async (IGitHubService gitHubService) =>
    {
        var repos = await gitHubService.GetUserRepositoriesAsync("sametcn99");

        return Results.Json(
            repos.Select(repo => new
            {
                name = repo.Name,
                description = repo.Description,
                language = repo.Language,
                fork = repo.Fork,
                html_url = repo.HtmlUrl,
                updated_at = repo.UpdatedAt,
            })
        );
    }
);

// JSON Feed (https://www.jsonfeed.org/version/1.1/)
app.MapGet(
    "/feed.json",
    async (HttpContext context, IContentService contentService) =>
    {
        var request = context.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var allContents = await contentService.GetAllContentsAsync();

        var items = allContents.Select(c => new
        {
            id = $"{baseUrl}{c.Href}",
            url = $"{baseUrl}{c.Href}",
            title = c.Title,
            summary = c.Summary,
            date_published = c.PublishedAt,
            tags = c.Tags,
        });

        var feed = new
        {
            version = "https://jsonfeed.org/version/1.1",
            title = "Samet Can Cıncık",
            home_page_url = baseUrl,
            feed_url = $"{baseUrl}/feed.json",
            items,
        };

        context.Response.Headers.CacheControl = "public, max-age=1800";
        return Results.Json(feed);
    }
);

app.Run();

static IResult FilterAndPaginate(
    List<PostModel> posts,
    string? search,
    string? tag,
    int? page,
    int? limit,
    string urlPrefix
)
{
    var filtered = posts.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var q = search.ToLowerInvariant();
        filtered = filtered.Where(p =>
            p.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || p.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
        );
    }

    if (!string.IsNullOrWhiteSpace(tag))
    {
        filtered = filtered.Where(p => p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }

    var filteredList = filtered.ToList();

    var total = filteredList.Count;
    var pageSize = Math.Clamp(limit ?? 20, 1, 100);
    var pageNum = Math.Max(page ?? 1, 1);
    var items = filteredList.Skip((pageNum - 1) * pageSize).Take(pageSize);

    return Results.Json(
        new
        {
            total,
            page = pageNum,
            limit = pageSize,
            data = items.Select(p => new
            {
                title = p.Title,
                slug = p.Slug,
                href = $"/{urlPrefix}/{p.Slug}",
                description = p.Description,
                publishDate = p.PublishDate?.ToString("yyyy-MM-dd"),
                tags = p.Tags,
                language = p.Language,
            }),
        }
    );
}
