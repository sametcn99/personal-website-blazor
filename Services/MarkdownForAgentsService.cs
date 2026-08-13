using System.Text.RegularExpressions;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

public class MarkdownForAgentsService : IMarkdownForAgentsService
{
    private readonly IContentService _contentService;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MarkdownForAgentsService> _logger;

    private static readonly Regex ValidSlugRegex = new(@"^[a-z0-9_-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidSections = new(StringComparer.OrdinalIgnoreCase)
        { "posts", "gists", "projects" };

    private static readonly HashSet<string> MarkdownRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/blog",
        "/gist",
        "/project",
        "/cv",
        "/readme",
        "/privacy-policy",
        "/support"
    };

    public MarkdownForAgentsService(
        IContentService contentService,
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory,
        ILogger<MarkdownForAgentsService> logger)
    {
        _contentService = contentService;
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string?> GetPageMarkdownAsync(string path)
    {
        var normalizedPath = path.TrimStart('/').ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizedPath))
            return await GetHomePageMarkdownAsync();

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length switch
        {
            1 when segments[0] == "blog" => await GetBlogListMarkdownAsync(),
            1 when segments[0] == "gist" => await GetGistListMarkdownAsync(),
            1 when segments[0] == "project" => await GetProjectListMarkdownAsync(),
            1 when segments[0] == "cv" => await GetCvMarkdownAsync(),
            1 when segments[0] == "readme" => await GetReadmeMarkdownAsync(),
            1 when segments[0] == "privacy-policy" => GetStaticPageMarkdown("privacy-policy", "Privacy Policy"),
            1 when segments[0] == "support" => GetStaticPageMarkdown("support", "Support"),
            2 when IsValidSection(segments[0]) && IsValidSlug(segments[1])
                => await GetContentPostMarkdownAsync(segments[0], segments[1]),
            _ => null
        };
    }

    public Task<int> EstimateTokenCountAsync(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return Task.FromResult(0);

        var wordCount = markdown.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var estimatedTokens = (int)Math.Ceiling(wordCount * 1.3);
        return Task.FromResult(estimatedTokens);
    }

    private async Task<string> GetHomePageMarkdownAsync()
    {
        var blogs = await _contentService.GetPostsAsync("posts");
        var gists = await _contentService.GetPostsAsync("gists");
        var projects = await _contentService.GetPostsAsync("projects");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: Samet Can Cıncık");
        sb.AppendLine("description: Personal website of Samet Can Cincik featuring blog posts, projects, gists, and links.");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Samet Can Cıncık");
        sb.AppendLine();
        sb.AppendLine("Welcome to my personal website.");
        sb.AppendLine();

        if (blogs.Any())
        {
            sb.AppendLine("## Blog Posts");
            sb.AppendLine();
            foreach (var post in blogs.OrderByDescending(GetEffectiveDate).Take(5))
            {
                sb.AppendLine($"- [{post.Title}](/blog/{post.Slug})");
                if (!string.IsNullOrEmpty(post.Description))
                    sb.AppendLine($"  {post.Description}");
            }
            sb.AppendLine();
        }

        if (gists.Any())
        {
            sb.AppendLine("## Technical Gists");
            sb.AppendLine();
            foreach (var post in gists.OrderByDescending(GetEffectiveDate).Take(5))
            {
                sb.AppendLine($"- [{post.Title}](/gist/{post.Slug})");
                if (!string.IsNullOrEmpty(post.Description))
                    sb.AppendLine($"  {post.Description}");
            }
            sb.AppendLine();
        }

        if (projects.Any())
        {
            sb.AppendLine("## Projects");
            sb.AppendLine();
            foreach (var post in projects.OrderByDescending(GetEffectiveDate).Take(5))
            {
                sb.AppendLine($"- [{post.Title}](/project/{post.Slug})");
                if (!string.IsNullOrEmpty(post.Description))
                    sb.AppendLine($"  {post.Description}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> GetBlogListMarkdownAsync()
    {
        var posts = await _contentService.GetPostsAsync("posts");
        return BuildListMarkdown(posts, "Blog Posts", "blog");
    }

    private async Task<string> GetGistListMarkdownAsync()
    {
        var posts = await _contentService.GetPostsAsync("gists");
        return BuildListMarkdown(posts, "Technical Gists", "gist");
    }

    private async Task<string> GetProjectListMarkdownAsync()
    {
        var posts = await _contentService.GetPostsAsync("projects");
        return BuildListMarkdown(posts, "Projects", "project");
    }

    private string BuildListMarkdown(List<PostModel> posts, string title, string urlPrefix)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {title} | Samet Can Cıncık");
        sb.AppendLine($"description: A collection of {title.ToLowerInvariant()}.");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        foreach (var post in posts.OrderByDescending(GetEffectiveDate))
        {
            sb.AppendLine($"## [{post.Title}](/{urlPrefix}/{post.Slug})");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(post.Description))
                sb.AppendLine(post.Description);
            sb.AppendLine();
            if (post.Tags.Length > 0)
            {
                sb.AppendLine($"**Tags:** {string.Join(", ", post.Tags)}");
                sb.AppendLine();
            }
            if (post.PublishDate.HasValue)
            {
                sb.AppendLine($"**Published:** {post.PublishDate.Value:yyyy-MM-dd}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private async Task<string?> GetContentPostMarkdownAsync(string section, string slug)
    {
        var post = await _contentService.GetPostAsync(section, slug);
        if (post is null)
            return null;

        var fsSection = section switch
        {
            "blog" => "posts",
            "gist" => "gists",
            "project" => "projects",
            _ => section
        };

        var filePath = Path.Combine(_env.ContentRootPath, "content", fsSection, $"{slug}.mdx");
        if (!File.Exists(filePath))
        {
            filePath = Path.Combine(_env.ContentRootPath, "content", fsSection, $"{slug}.md");
            if (!File.Exists(filePath))
                return null;
        }

        var rawMarkdown = await File.ReadAllTextAsync(filePath);
        return rawMarkdown;
    }

    private async Task<string?> GetCvMarkdownAsync()
    {
        var path = Path.Combine(_env.ContentRootPath, "content", "cv.mdx");
        if (!File.Exists(path))
            return null;

        var markdown = await File.ReadAllTextAsync(path);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: CV | Samet Can Cıncık");
        sb.AppendLine("description: CV, experience, skills, and downloadable resume files.");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(markdown);

        return sb.ToString();
    }

    private async Task<string?> GetReadmeMarkdownAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GitHub");
            var markdown = await client.GetStringAsync(
                "https://raw.githubusercontent.com/sametcn99/sametcn99/main/README.md");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine("title: README | Samet Can Cıncık");
            sb.AppendLine("description: GitHub profile README with background, interests, and current focus areas.");
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(markdown);

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch README for markdown negotiation");
            return null;
        }
    }

    private static string GetStaticPageMarkdown(string slug, string title)
    {
        return $"---\ntitle: {title} | Samet Can Cıncık\n---\n\n# {title}\n\nThis page is available in HTML format.\n";
    }

    private static bool IsValidSection(string section) =>
        ValidSections.Contains(section);

    private static bool IsValidSlug(string slug) =>
        ValidSlugRegex.IsMatch(slug);

    private static DateTime? GetEffectiveDate(PostModel post) => post.UpdatedAt ?? post.PublishDate;
}
