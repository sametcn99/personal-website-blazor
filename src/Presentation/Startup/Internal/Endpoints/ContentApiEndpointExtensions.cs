using personal_website_blazor.Application.Abstractions;
using personal_website_blazor.Domain.Entities;
using Markdig;

namespace personal_website_blazor.Presentation.Startup.Internal.Endpoints;

internal static class ContentApiEndpointExtensions
{
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
                if (string.IsNullOrWhiteSpace(q))
                    return Results.Json(new List<SearchResult>());

                var results = await contentService.SearchAsync(q, section);
                return Results.Json(results);
            }
        );

        app.MapGet(
            "/api/content/{section}",
            async (IContentService contentService, string section) =>
            {
                var posts = await contentService.GetPostsAsync(section);
                return Results.Json(posts);
            }
        );

        app.MapGet(
            "/api/content/{section}/{slug}",
            async (IContentService contentService, string section, string slug) =>
            {
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
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();

                return Results.Json(new { html = Markdown.ToHtml(markdown, pipeline) });
            }
        );

        app.MapGet(
            "/api/readme",
            async (IHttpClientFactory httpClientFactory) =>
            {
                var client = httpClientFactory.CreateClient("GitHub");
                var markdown = await client.GetStringAsync(
                    "https://raw.githubusercontent.com/sametcn99/sametcn99/refs/heads/main/README.md"
                );

                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();

                return Results.Json(new { html = Markdown.ToHtml(markdown, pipeline) });
            }
        );

        app.MapGet(
            "/api/repos/{slug}",
            async (IHttpClientFactory httpClientFactory, string slug) =>
            {
                var client = httpClientFactory.CreateClient("GitHub");
                var response = await client.GetAsync($"https://api.github.com/repos/sametcn99/{slug}");
                if (!response.IsSuccessStatusCode)
                {
                    return Results.NotFound();
                }

                var repo = await response.Content.ReadFromJsonAsync<RepoRedirectDto>();
                return repo is null ? Results.NotFound() : Results.Json(repo);
            }
        );

        app.MapGet(
            "/api/blog",
            async (
                IContentService contentService,
                string? search,
                string? tag,
                int? page,
                int? limit
            ) =>
            {
                var posts = await contentService.GetPostsAsync("posts");
                return FilterAndPaginate(posts, search, tag, page, limit, "blog");
            }
        );

        app.MapGet(
            "/api/gists",
            async (
                IContentService contentService,
                string? search,
                string? tag,
                int? page,
                int? limit
            ) =>
            {
                var posts = await contentService.GetPostsAsync("gists");
                return FilterAndPaginate(posts, search, tag, page, limit, "gist");
            }
        );

        app.MapGet(
            "/api/projects",
            async (
                IContentService contentService,
                string? search,
                string? tag,
                int? page,
                int? limit
            ) =>
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

        return app;
    }

    private static IResult FilterAndPaginate(
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
                    updatedAt = p.UpdatedAt?.ToString("yyyy-MM-dd"),
                    tags = p.Tags,
                    language = p.Language,
                }),
            }
        );
    }

    private sealed class RepoRedirectDto
    {
        public string Html_url { get; set; } = string.Empty;
        public string Full_name { get; set; } = string.Empty;
    }
}
