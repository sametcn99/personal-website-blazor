using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

public class MarkdownForAgentsService : IMarkdownForAgentsService
{
    private readonly IContentService _contentService;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MarkdownForAgentsService> _logger;
    private readonly IProfileService _profileService;
    private readonly IGitHubService _gitHubService;
    private readonly IMemoryCache _cache;

    private static readonly Regex ValidSlugRegex = new(@"^[a-z0-9_-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidSections = new(StringComparer.OrdinalIgnoreCase)
        { "posts", "gists", "projects" };

    private static readonly HashSet<string> MarkdownRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/blog",
        "/gist",
        "/project",
        "/content",
        "/repo",
        "/cv",
        "/readme",
        "/about",
        "/profile",
        "/timeline",
        "/skills",
        "/privacy-policy",
        "/support"
    };

    public MarkdownForAgentsService(
        IContentService contentService,
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory,
        ILogger<MarkdownForAgentsService> logger,
        IProfileService profileService,
        IGitHubService gitHubService,
        IMemoryCache cache)
    {
        _contentService = contentService;
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _profileService = profileService;
        _gitHubService = gitHubService;
        _cache = cache;
    }

    public async Task<string?> GetPageMarkdownAsync(string path)
    {
        var normalizedPath = NormalizeMarkdownPath(path);

        if (string.IsNullOrEmpty(normalizedPath))
            return await GetHomePageMarkdownAsync();

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length switch
        {
            1 when segments[0] == "blog" => await GetBlogListMarkdownAsync(),
            1 when segments[0] == "gist" => await GetGistListMarkdownAsync(),
            1 when segments[0] == "project" => await GetProjectListMarkdownAsync(),
            1 when segments[0] == "content" => await GetAllContentListMarkdownAsync(),
            1 when segments[0] == "repo" => await GetRepositoriesMarkdownAsync(),
            1 when segments[0] == "cv" => await GetCvMarkdownAsync(),
            1 when segments[0] == "readme" => await GetReadmeMarkdownAsync(),
            1 when segments[0] is "about" or "profile" => await _profileService.GetProfileMarkdownAsync(string.Empty),
            1 when segments[0] == "timeline" => await GetTimelineMarkdownAsync(),
            1 when segments[0] == "skills" => await GetSkillsMarkdownAsync(),
            1 when segments[0] == "privacy-policy" => GetStaticPageMarkdown("privacy-policy", "Privacy Policy"),
            1 when segments[0] == "support" => GetStaticPageMarkdown("support", "Support"),
            2 when IsValidRouteSection(segments[0]) && IsValidSlug(segments[1])
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

    private static string NormalizeMarkdownPath(string path)
    {
        var normalizedPath = path.Trim().TrimStart('/').ToLowerInvariant();
        if (normalizedPath.EndsWith(".md", StringComparison.Ordinal))
            normalizedPath = normalizedPath[..^3].TrimEnd('/');

        return normalizedPath == "index" ? string.Empty : normalizedPath;
    }

    private async Task<string> GetTimelineMarkdownAsync()
    {
        var profile = await _profileService.GetProfileAsync();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine("title: Timeline | Samet Can Cıncık");
        sb.AppendLine("description: Career and education timeline for Samet Can Cıncık.");
        sb.AppendLine("type: timeline");
        sb.AppendLine($"lastUpdated: {profile.LastUpdated}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Timeline");
        sb.AppendLine();

        foreach (var item in profile.Experience)
        {
            sb.AppendLine($"## {item.Role} — {item.Organization}");
            sb.AppendLine();
            sb.AppendLine($"**Period:** {item.StartDate} to {item.EndDate}");
            sb.AppendLine($"**Location:** {item.Location}");
            sb.AppendLine();
            foreach (var highlight in item.Highlights)
                sb.AppendLine($"- {highlight}");
            sb.AppendLine();
        }

        foreach (var item in profile.Education)
        {
            sb.AppendLine($"## {item.Program} — {item.Institution}");
            sb.AppendLine();
            sb.AppendLine($"**Type:** {item.Level}");
            sb.AppendLine($"**Period:** {item.StartDate} to {item.EndDate}");
            sb.AppendLine();
            foreach (var highlight in item.Highlights)
                sb.AppendLine($"- {highlight}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> GetSkillsMarkdownAsync()
    {
        var profile = await _profileService.GetProfileAsync();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine("title: Skills | Samet Can Cıncık");
        sb.AppendLine("description: Technical skills, areas of interest, and languages.");
        sb.AppendLine("type: skills");
        sb.AppendLine($"lastUpdated: {profile.LastUpdated}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# Skills");
        sb.AppendLine();
        AppendBulletSection(sb, "Technical Skills", profile.Skills);
        AppendBulletSection(sb, "Areas Of Interest", profile.AreasOfInterest);
        AppendBulletSection(sb, "Languages", profile.Languages);

        return sb.ToString();
    }

    private async Task<string> GetAllContentListMarkdownAsync()
    {
        var contents = await GetLlmsContentsAsync();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine("title: All Content | Samet Can Cıncık");
        sb.AppendLine("description: Complete index of published blog posts, technical gists, and project records.");
        sb.AppendLine("type: content-index");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# All Content");
        sb.AppendLine();
        AppendContentSection(sb, contents, "blog", "Blog Posts", string.Empty);
        AppendContentSection(sb, contents, "gist", "Technical Gists", string.Empty);
        AppendContentSection(sb, contents, "project", "Projects", string.Empty);
        return sb.ToString();
    }

    private async Task<string> GetRepositoriesMarkdownAsync()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: GitHub Repositories | Samet Can Cıncık");
        sb.AppendLine("description: Public GitHub repositories for sametcn99.");
        sb.AppendLine("type: repository-index");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("# GitHub Repositories");
        sb.AppendLine();

        try
        {
            var repositories = await _gitHubService.GetUserRepositoriesAsync("sametcn99");
            foreach (var repository in repositories)
            {
                sb.AppendLine($"## [{repository.Name}]({repository.HtmlUrl})");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(repository.Description))
                    sb.AppendLine(repository.Description);
                sb.AppendLine();
                sb.AppendLine($"- Language: {repository.Language ?? "Not specified"}");
                sb.AppendLine($"- Stars: {repository.StargazersCount}");
                sb.AppendLine($"- Forks: {repository.ForksCount}");
                sb.AppendLine($"- Archived: {repository.Archived}");
                sb.AppendLine($"- Topics: {(repository.Topics.Length > 0 ? string.Join(", ", repository.Topics) : "None listed")}");
                sb.AppendLine($"- Last pushed: {repository.PushedAt?.ToString("yyyy-MM-dd") ?? "Unknown"}");
                sb.AppendLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate repository Markdown");
            sb.AppendLine("GitHub repository data is temporarily unavailable.");
        }

        return sb.ToString();
    }

    public async Task<string> GetLlmsFullTxtAsync(string baseUrl)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var contents = await GetLlmsContentsAsync();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Samet Can Cincik — Full Agent Context");
        sb.AppendLine();
        sb.AppendLine("> Full Markdown context for researching Samet Can Cıncık's professional profile, authored content, projects, and public GitHub work.");
        sb.AppendLine();
        sb.AppendLine("This document is generated from the profile source, content metadata, published Markdown files, and the public GitHub repository snapshot. Prefer the individual linked pages when citing a specific claim.");
        sb.AppendLine();

        sb.AppendLine("## Author Profile");
        sb.AppendLine();
        sb.AppendLine(await _profileService.GetProfileMarkdownAsync(normalizedBaseUrl));

        sb.AppendLine("## GitHub Profile README");
        sb.AppendLine();
        sb.AppendLine(await GetReadmeMarkdownAsync() ?? "GitHub profile README is temporarily unavailable.");

        sb.AppendLine("## Published Content");
        sb.AppendLine();
        foreach (var content in contents)
        {
            var routeParts = content.Href.Trim('/').Split('/');
            if (routeParts.Length != 2)
                continue;

            sb.AppendLine($"### [{content.Title}]({normalizedBaseUrl}{content.Href})");
            sb.AppendLine();
            var markdown = await GetContentPostMarkdownAsync(routeParts[0], routeParts[1]);
            sb.AppendLine(markdown ?? content.Summary);
            sb.AppendLine();
        }

        sb.AppendLine("## GitHub Repository Snapshot");
        sb.AppendLine();
        try
        {
            var repositories = await _gitHubService.GetUserRepositoriesAsync("sametcn99");
            foreach (var repository in repositories)
            {
                sb.AppendLine($"### [{repository.Name}]({repository.HtmlUrl})");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(repository.Description))
                    sb.AppendLine(repository.Description);
                sb.AppendLine();
                sb.AppendLine($"- Language: {repository.Language ?? "Not specified"}");
                sb.AppendLine($"- Stars: {repository.StargazersCount}");
                sb.AppendLine($"- Forks: {repository.ForksCount}");
                sb.AppendLine($"- Archived: {repository.Archived}");
                sb.AppendLine($"- Topics: {(repository.Topics.Length > 0 ? string.Join(", ", repository.Topics) : "None listed")}");
                sb.AppendLine($"- Fork: {repository.Fork}");
                sb.AppendLine($"- Last pushed: {repository.PushedAt?.ToString("yyyy-MM-dd") ?? "Unknown"}");
                sb.AppendLine();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to include GitHub repositories in llms-full.txt");
            sb.AppendLine("GitHub repository data is temporarily unavailable. Use the public GitHub profile link in the main index.");
        }

        return sb.ToString();
    }

    public async Task<string> GetLlmsTxtAsync(string baseUrl)
    {
        var contents = await GetLlmsContentsAsync();
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Samet Can Cincik");
        sb.AppendLine();
        sb.AppendLine("> Personal website and public knowledge archive of Samet Can Cincik. The site contains software engineering notes, technical gists, project documentation, GitHub repository data, and machine-readable feeds.");
        sb.AppendLine();
        sb.AppendLine($"Use this file as the navigation index for the public content on `{normalizedBaseUrl}`. Content pages are available as HTML by default and can be requested as Markdown with the `Accept: text/markdown` HTTP header.");
        sb.AppendLine();

        await AppendAuthorSectionAsync(sb, normalizedBaseUrl);
        AppendArchiveOverview(sb, contents);

        AppendLinkSection(sb, "Start Here", new[]
        {
            ($"{normalizedBaseUrl}/index.md", "Home", "Overview of the site and the latest content."),
            ($"{normalizedBaseUrl}/content.md", "All content", "Combined archive of blog posts, gists, and projects."),
            ($"{normalizedBaseUrl}/blog.md", "Blog posts", "Long-form writing, technical explanations, and essays."),
            ($"{normalizedBaseUrl}/gist.md", "Technical gists", "Focused guides, references, scripts, and configuration examples."),
            ($"{normalizedBaseUrl}/project.md", "Projects", "Software projects and project documentation."),
            ($"{normalizedBaseUrl}/about.md", "About", "Canonical structured profile of Samet Can Cıncık."),
            ($"{normalizedBaseUrl}/timeline.md", "Timeline", "Career and education timeline."),
            ($"{normalizedBaseUrl}/skills.md", "Skills", "Technical skills, interests, and languages."),
            ($"{normalizedBaseUrl}/readme.md", "About / README", "Public profile and background information from the author's GitHub README."),
            ($"{normalizedBaseUrl}/cv.md", "CV", "Curriculum vitae."),
            ($"{normalizedBaseUrl}/repo.md", "Repositories", "Public GitHub repositories for sametcn99."),
            ($"{normalizedBaseUrl}/link", "Links", "Curated external links."),
            ($"{normalizedBaseUrl}/support", "Support", "Ways to support the author."),
            ($"{normalizedBaseUrl}/privacy-policy.md", "Privacy policy", "Privacy and analytics information."),
        });

        AppendContentSection(sb, contents, "blog", "Blog Posts", normalizedBaseUrl);
        AppendContentSection(sb, contents, "gist", "Technical Gists", normalizedBaseUrl);
        AppendContentSection(sb, contents, "project", "Projects", normalizedBaseUrl);

        AppendLinkSection(sb, "Machine-Readable Content", new[]
        {
            ($"{normalizedBaseUrl}/openapi.json", "OpenAPI document", "OpenAPI description of the public HTTP API."),
            ($"{normalizedBaseUrl}/feed.json", "JSON Feed", "JSON Feed 1.1 containing posts, gists, and projects."),
            ($"{normalizedBaseUrl}/rss.xml", "RSS feed", "RSS feed of the latest content."),
            ($"{normalizedBaseUrl}/sitemap.xml", "Sitemap", "XML sitemap containing public site URLs."),
            ($"{normalizedBaseUrl}/.well-known/api-catalog", "API catalog", "Linkset metadata connecting API resources to OpenAPI and human documentation."),
            ($"{normalizedBaseUrl}/.well-known/acp.json", "ACP discovery", "Agent Communication Protocol discovery metadata."),
            ($"{normalizedBaseUrl}/.well-known/mcp/server-card.json", "MCP Server Card", "MCP HTTP server discovery metadata."),
            ($"{normalizedBaseUrl}/.well-known/agent-skills/index.json", "Agent skills index", "Discoverable agent skill metadata for reading website content."),
            ($"{normalizedBaseUrl}/auth.md", "Agent authentication documentation", "Authentication and agent access documentation."),
            ($"{normalizedBaseUrl}/llms-full.txt", "Full agent context", "Full Markdown context containing profile, published content, and a GitHub repository snapshot."),
        });

        sb.AppendLine("## Content API");
        sb.AppendLine();
        sb.AppendLine("All API responses are JSON unless otherwise noted. The `section` path parameter accepts `posts`, `gists`, or `projects`; individual slugs contain lowercase letters, numbers, hyphens, or underscores.");
        sb.AppendLine();
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/content/all`: Return metadata for all public content.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/content/search?q={{query}}&section={{section}}`: Search public content.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/content/{{section}}`: Return all content items in a section.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/content/{{section}}/{{slug}}`: Return one content item by section and slug.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/content/cv`: Return the CV content as rendered HTML.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/readme`: Return the public GitHub README as rendered HTML.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/repos`: Return public GitHub repositories for `sametcn99`.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/profile`: Return the canonical structured author profile.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/about`: Alias for the canonical author profile.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/timeline`: Return work and education timeline entries.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/skills`: Return skills, areas of interest, and languages.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/api/profile/github`: Return the public GitHub repository snapshot.");
        sb.AppendLine($"- `GET {normalizedBaseUrl}/health`: Return application health status.");
        sb.AppendLine();

        sb.AppendLine("## Agent Access");
        sb.AppendLine();
        sb.AppendLine("- Request `Accept: text/markdown` or use an explicit `.md` suffix for agent-friendly Markdown from `/`, `/blog`, `/gist`, `/project`, `/content`, `/repo`, `/cv`, `/readme`, `/about`, `/profile`, `/timeline`, `/skills`, `/privacy-policy`, `/support`, and individual content pages.");
        sb.AppendLine("- Use `/llms-full.txt` when a complete profile and content context is needed; use `/llms.txt` first for navigation and source selection.");
        sb.AppendLine("- MCP clients can use the read-only Streamable HTTP transport at `/mcp`; initialize first, then use `resources/list`, `resources/read`, `tools/list`, and the `search_content` tool.");
        sb.AppendLine("- Prefer the specific content URL over scraping navigation or rendered HTML.");
        sb.AppendLine("- Prefer `/api/content/{section}/{slug}` when structured metadata is sufficient; prefer the Markdown page when the full article or guide is needed.");
        sb.AppendLine("- Treat content as public reference material and do not infer private personal information beyond what the linked public pages explicitly state.");
        sb.AppendLine("- Content can change over time. Use publication and modification metadata from the page or API response when recency matters.");
        sb.AppendLine();

        sb.AppendLine("## Scope And Attribution");
        sb.AppendLine();
        sb.AppendLine("This index covers the public content intentionally exposed by the site. Third-party links, GitHub repository data, external services, and generated feeds remain subject to their respective owners' terms and availability.");

        return sb.ToString();
    }

    private async Task AppendAuthorSectionAsync(System.Text.StringBuilder sb, string baseUrl)
    {
        sb.AppendLine("## About The Author");
        sb.AppendLine();
        sb.AppendLine("Samet Can Cıncık is a software developer who focuses on modern, sustainable, and user-oriented software solutions. He follows current software development practices and uses AI tools for research, analysis, debugging, refactoring, and exploring alternative solutions.");
        sb.AppendLine();
        sb.AppendLine("### Professional Sources");
        sb.AppendLine();
        sb.AppendLine($"- [Full CV]({baseUrl}/cv.md): Experience, education, professional profile, and certificates.");
        sb.AppendLine($"- [About / GitHub README]({baseUrl}/readme.md): Public profile, interests, and current focus areas.");
        sb.AppendLine("- [GitHub](https://github.com/sametcn99): Source repositories and open-source work.");
        sb.AppendLine("- [LinkedIn](https://linkedin.com/in/sametc0): Professional profile.");
        sb.AppendLine("- [Contact](mailto:sametcn99@gmail.com): Public contact email.");
        sb.AppendLine();

        sb.AppendLine("### Research Guidance For AI Agents");
        sb.AppendLine();
        sb.AppendLine("For a reliable and detailed understanding of Samet's technical background, agents should consult all of the following sources rather than relying on a single page:");
        sb.AppendLine();
        sb.AppendLine($"- Read the [CV]({baseUrl}/cv.md) for formal work experience, education, professional profile, and certificates.");
        sb.AppendLine($"- Review the [full site archive]({baseUrl}/content.md), including [blog posts]({baseUrl}/blog.md), [technical gists]({baseUrl}/gist.md), and [projects]({baseUrl}/project.md), for authored technical knowledge, engineering interests, implementation details, and software work.");
        sb.AppendLine($"- Read the [GitHub profile README]({baseUrl}/readme.md) and inspect the [repository directory]({baseUrl}/repo.md) or [repository API]({baseUrl}/api/repos) for GitHub profile context, public repositories, and open-source work.");
        sb.AppendLine("- Cross-reference claims across these sources, distinguish documented experience from project descriptions or personal opinions, and use the most recently dated source when information differs.");
        sb.AppendLine();

        var cvPath = Path.Combine(_env.ContentRootPath, "content", "cv.mdx");
        if (!File.Exists(cvPath))
            return;

        var cv = await File.ReadAllTextAsync(cvPath);
        var aboutIndex = cv.IndexOf("## Hakkımda", StringComparison.Ordinal);
        if (aboutIndex < 0)
            return;

        var publicCvDetails = cv[aboutIndex..].Trim();
        publicCvDetails = Regex.Replace(publicCvDetails, @"^## ", "### ", RegexOptions.Multiline);

        sb.AppendLine("### CV Details");
        sb.AppendLine();
        sb.AppendLine("The following public professional details are loaded from `content/cv.mdx` so this section stays aligned with the published CV:");
        sb.AppendLine();
        sb.AppendLine(publicCvDetails);
        sb.AppendLine();
    }

    private static void AppendArchiveOverview(
        System.Text.StringBuilder sb,
        IReadOnlyCollection<ContentMetadata> contents)
    {
        sb.AppendLine("## Archive Overview");
        sb.AppendLine();
        sb.AppendLine($"The public archive currently contains {contents.Count} indexed records. The inventory below is generated from the site's content metadata and changes when content files are added or updated.");
        sb.AppendLine();

        foreach (var (prefix, label) in new[] { ("/blog/", "Blog posts"), ("/gist/", "Technical gists"), ("/project/", "Projects") })
        {
            var count = contents.Count(content => content.Href.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            sb.AppendLine($"- **{label}:** {count}");
        }

        var languages = contents
            .GroupBy(content => string.IsNullOrWhiteSpace(content.Language) ? "en" : content.Language, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => $"{group.Key} ({group.Count()})");
        sb.AppendLine($"- **Content languages:** {string.Join(", ", languages)}");
        sb.AppendLine();

        var tags = contents
            .SelectMany(content => content.Tags.Concat(content.Technologies).Concat(content.Topics))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .GroupBy(tag => tag.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => $"{group.Key} ({group.Count()})");

        if (tags.Any())
        {
            sb.AppendLine("### Recurring Topics And Technologies");
            sb.AppendLine();
            sb.AppendLine("Tags are taken from published content metadata and indicate recurring subjects, tools, frameworks, and domains:");
            sb.AppendLine();
            sb.AppendLine($"- {string.Join(", ", tags)}");
            sb.AppendLine();
        }
    }

    private async Task<List<ContentMetadata>> GetLlmsContentsAsync()
    {
        var contents = new List<ContentMetadata>();
        var sections = new[] { (FileSection: "posts", UrlPrefix: "blog"), (FileSection: "gists", UrlPrefix: "gist"), (FileSection: "projects", UrlPrefix: "project") };

        foreach (var section in sections)
        {
            var posts = await _contentService.GetPostMetadataListAsync(section.FileSection);
            contents.AddRange(posts.Select(post => new ContentMetadata
            {
                Title = post.Title,
                Href = $"/{section.UrlPrefix}/{post.Slug}",
                PublishedAt = post.PublishDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                UpdatedAt = post.UpdatedAt?.ToString("yyyy-MM-dd"),
                Summary = post.Description,
                Tags = post.Tags,
                Technologies = post.Technologies,
                Topics = post.Topics,
                RelatedProjects = post.RelatedProjects,
                RelatedPosts = post.RelatedPosts,
                CanonicalUrl = post.CanonicalUrl,
                ContentType = post.ContentType,
                Status = post.Status,
                Language = post.Language,
            }));
        }

        return contents
            .OrderByDescending(content => content.UpdatedAt ?? content.PublishedAt)
            .ToList();
    }

    private static void AppendLinkSection(
        System.Text.StringBuilder sb,
        string heading,
        IEnumerable<(string Url, string Title, string Description)> links)
    {
        sb.AppendLine($"## {heading}");
        sb.AppendLine();

        foreach (var link in links)
            sb.AppendLine($"- [{link.Title}]({link.Url}): {link.Description}");

        sb.AppendLine();
    }

    private static void AppendBulletSection(System.Text.StringBuilder sb, string heading, IEnumerable<string> values)
    {
        sb.AppendLine($"## {heading}");
        sb.AppendLine();
        foreach (var value in values)
            sb.AppendLine($"- {value}");
        sb.AppendLine();
    }

    private static void AppendContentSection(
        System.Text.StringBuilder sb,
        IEnumerable<ContentMetadata> contents,
        string urlPrefix,
        string heading,
        string baseUrl)
    {
        sb.AppendLine($"## {heading}");
        sb.AppendLine();

        var sectionContents = contents.Where(content => content.Href.StartsWith($"/{urlPrefix}/", StringComparison.OrdinalIgnoreCase));
        foreach (var content in sectionContents)
        {
            var title = content.Title.Replace("[", "\\[").Replace("]", "\\]");
            var description = NormalizeText(content.Summary);
            var metadata = new List<string>();

            if (!string.IsNullOrWhiteSpace(content.PublishedAt))
                metadata.Add($"Published: {content.PublishedAt}");
            if (!string.IsNullOrWhiteSpace(content.UpdatedAt))
                metadata.Add($"Updated: {content.UpdatedAt}");
            if (content.Tags.Length > 0)
                metadata.Add($"Tags: {string.Join(", ", content.Tags)}");
            if (content.Technologies.Length > 0)
                metadata.Add($"Technologies: {string.Join(", ", content.Technologies)}");
            if (content.Topics.Length > 0)
                metadata.Add($"Topics: {string.Join(", ", content.Topics)}");
            if (content.RelatedProjects.Length > 0)
                metadata.Add($"Related projects: {string.Join(", ", content.RelatedProjects)}");

            sb.Append($"- [{title}]({baseUrl}{content.Href}.md)");
            if (!string.IsNullOrWhiteSpace(description))
                sb.Append($": {description}");
            if (metadata.Count > 0)
                sb.Append($" ({string.Join("; ", metadata)})");
            sb.AppendLine();
        }

        sb.AppendLine();
    }

    private static string NormalizeText(string value) =>
        Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();

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
        var fsSection = section switch
        {
            "blog" => "posts",
            "gist" => "gists",
            "project" => "projects",
            _ => section
        };

        var post = await _contentService.GetPostAsync(fsSection, slug);
        if (post is null)
            return null;

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
        const string cacheKey = "github-profile-readme-markdown";
        string? markdown;
        if (!_cache.TryGetValue(cacheKey, out markdown))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GitHub");
                markdown = await client.GetStringAsync(
                    "https://raw.githubusercontent.com/sametcn99/sametcn99/main/README.md");

                _cache.Set(cacheKey, markdown, TimeSpan.FromHours(12));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch README for markdown negotiation");
                return null;
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: README | Samet Can Cıncık");
        sb.AppendLine("description: GitHub profile README with background, interests, and current focus areas.");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(markdown);

        return sb.ToString();
    }

    private static string GetStaticPageMarkdown(string slug, string title)
    {
        return $"---\ntitle: {title} | Samet Can Cıncık\n---\n\n# {title}\n\nThis page is available in HTML format.\n";
    }

    private static bool IsValidSection(string section) =>
        ValidSections.Contains(section);

    private static bool IsValidRouteSection(string section) =>
        section is "blog" or "gist" or "project";

    private static bool IsValidSlug(string slug) =>
        ValidSlugRegex.IsMatch(slug);

    private static DateTime? GetEffectiveDate(PostModel post) => post.UpdatedAt ?? post.PublishDate;
}
