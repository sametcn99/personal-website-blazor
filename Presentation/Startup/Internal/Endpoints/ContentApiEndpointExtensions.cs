using personal_website_blazor.Core.Application.Abstractions;
using personal_website_blazor.Core.Domain.Entities;

namespace personal_website_blazor.Presentation.Startup.Internal.Endpoints;

internal static class ContentApiEndpointExtensions
{
    internal static WebApplication MapContentApiEndpoints(this WebApplication app)
    {
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
                    tags = p.Tags,
                    language = p.Language,
                }),
            }
        );
    }
}
