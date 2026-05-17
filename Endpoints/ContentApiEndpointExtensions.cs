using System.Text.RegularExpressions;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;
using Markdig;

namespace personal_website_blazor.Endpoints;

internal static class ContentApiEndpointExtensions
{
    private static readonly Regex ValidSlugRegex = new(@"^[a-z0-9_-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidSections = new(StringComparer.OrdinalIgnoreCase)
        { "posts", "gists", "projects", "links" };

    private static readonly MarkdownPipeline CvPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    internal static WebApplication MapContentApiEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/api/content/all",
            async (IContentService contentService) =>
            {
                var contents = await contentService.GetAllContentsAsync();
                return Results.Json(contents);
            }
        );

        app.MapGet(
            "/api/content/search",
            async (IContentService contentService, string? q, string? section) =>
            {
                if (string.IsNullOrWhiteSpace(q) || q.Length > 200)
                    return Results.Json(new List<SearchResult>());

                var results = await contentService.SearchAsync(q, section);
                return Results.Json(results);
            }
        );

        app.MapGet(
            "/api/content/{section}",
            async (IContentService contentService, string section) =>
            {
                if (!ValidSections.Contains(section))
                    return Results.NotFound();

                var posts = await contentService.GetPostsAsync(section);
                return Results.Json(posts);
            }
        );

        app.MapGet(
            "/api/content/{section}/{slug}",
            async (IContentService contentService, string section, string slug) =>
            {
                if (!ValidSections.Contains(section) || !ValidSlugRegex.IsMatch(slug))
                    return Results.NotFound();

                var post = await contentService.GetPostAsync(section, slug);
                return post is null ? Results.NotFound() : Results.Json(post);
            }
        );

        app.MapGet(
            "/api/content/cv",
            async (IWebHostEnvironment env) =>
            {
                var path = Path.Combine(env.ContentRootPath, "content", "cv.mdx");
                if (!File.Exists(path))
                {
                    return Results.NotFound();
                }

                var markdown = await File.ReadAllTextAsync(path);
                return Results.Json(new { html = Markdown.ToHtml(markdown, CvPipeline) });
            }
        );

        app.MapGet(
            "/api/readme",
            async (IHttpClientFactory httpClientFactory) =>
            {
                var client = httpClientFactory.CreateClient("GitHub");
                var markdown = await client.GetStringAsync(
                    "https://raw.githubusercontent.com/sametcn99/sametcn99/main/README.md");
                return Results.Content(markdown, "text/markdown");
            }
        );

        return app;
    }
}
