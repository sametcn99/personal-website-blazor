using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

public sealed class McpQueryService : IMcpQueryService
{
    private readonly IContentService _contentService;
    private readonly IProfileService _profileService;

    public McpQueryService(IContentService contentService, IProfileService profileService)
    {
        _contentService = contentService;
        _profileService = profileService;
    }

    public Task<ProfileDocument> GetProfileAsync() => _profileService.GetProfileAsync();

    public async Task<McpPage<McpContentSummary>> ListProjectsAsync(McpProjectFilter filter)
    {
        var projects = (await _contentService.GetAllContentsAsync())
            .Where(item => string.Equals(item.ContentType, "project", StringComparison.OrdinalIgnoreCase)
                || item.Href.StartsWith("/project/", StringComparison.OrdinalIgnoreCase))
            .Where(item => MatchesProject(item, filter))
            .Select(ToSummary)
            .ToList();

        return Page(projects, filter.Limit, filter.Cursor);
    }

    public async Task<McpContentDocument?> GetContentAsync(string section, string slug, bool includeBody, bool includeRelated)
    {
        var normalizedSection = NormalizeSection(section);
        var post = await _contentService.GetPostAsync(normalizedSection, slug);
        if (post is null)
            return null;

        var document = ToDocument(post, includeBody);
        if (includeRelated)
            document.RelatedContent = await GetRelatedContentAsync(normalizedSection, slug, 10);
        return document;
    }

    public async Task<McpPage<McpContentSummary>> SearchContentAsync(McpSearchFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Query))
            throw new ArgumentException("search_content requires a non-empty query.", nameof(filter));

        var query = filter.Query.Trim();
        var matches = (await _contentService.GetAllContentsAsync())
            .Where(item => MatchesSearch(item, filter, query))
            .Select(ToSummary)
            .ToList();

        return Page(matches, filter.Limit, filter.Cursor);
    }

    public async Task<IReadOnlyList<TimelineEntry>> GetTimelineAsync()
    {
        var profile = await _profileService.GetProfileAsync();
        return profile.Experience
            .Select(item => new TimelineEntry
            {
                Type = "experience",
                Organization = item.Organization,
                Role = item.Role,
                Location = item.Location,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Highlights = item.Highlights,
            })
            .Concat(profile.Education.Select(item => new TimelineEntry
            {
                Type = "education",
                Organization = item.Institution,
                Role = item.Program,
                Level = item.Level,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Highlights = item.Highlights,
            }))
            .OrderByDescending(item => item.StartDate)
            .ToArray();
    }

    public async Task<McpSkillsResult> GetSkillsAsync()
    {
        var profile = await _profileService.GetProfileAsync();
        return new McpSkillsResult
        {
            Skills = profile.Skills,
            AreasOfInterest = profile.AreasOfInterest,
            Languages = profile.Languages,
            PublicNotes = profile.PublicNotes,
            LastUpdated = profile.LastUpdated,
        };
    }

    public async Task<McpTaxonomyResult> GetTaxonomyAsync(string[] sections)
    {
        var content = (await _contentService.GetAllContentsAsync())
            .Where(item => !sections.Any()
                || sections.Any(section => string.Equals(GetSection(item.Href), NormalizeSection(section), StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        return new McpTaxonomyResult
        {
            Tags = CountValues(content.SelectMany(item => item.Tags)),
            Technologies = CountValues(content.SelectMany(item => item.Technologies)),
            Topics = CountValues(content.SelectMany(item => item.Topics)),
            Types = CountValues(content.Select(item => item.ContentType)),
            Languages = CountValues(content.Select(item => item.Language)),
        };
    }

    public async Task<McpRelatedContentResult?> GetRelatedContentAsync(string section, string slug, int limit)
    {
        var normalizedSection = NormalizeSection(section);
        var post = await _contentService.GetPostAsync(normalizedSection, slug);
        if (post is null)
            return null;

        var all = (await _contentService.GetAllContentsAsync()).Select(ToSummary).ToArray();
        var projects = ResolveReferences(post.RelatedProjects, all, "project", limit);
        var posts = ResolveReferences(post.RelatedPosts, all, "blog", limit);

        return new McpRelatedContentResult
        {
            Source = ToSummary(post),
            Projects = projects,
            Posts = posts,
        };
    }

    private static bool MatchesProject(ContentMetadata item, McpProjectFilter filter) =>
        (string.IsNullOrWhiteSpace(filter.Query)
            || Contains(item.Title, filter.Query)
            || Contains(item.Summary, filter.Query))
        && MatchesAny(item.Tags, filter.Tags)
        && MatchesAny(item.Technologies, filter.Technologies)
        && MatchesAny(item.Topics, filter.Topics)
        && (string.IsNullOrWhiteSpace(filter.Language)
            || string.Equals(item.Language, filter.Language, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesSearch(ContentMetadata item, McpSearchFilter filter, string query) =>
        (Contains(item.Title, query)
            || Contains(item.Summary, query)
            || Contains(item.SearchableText, query)
            || ContainsAny(item.Tags, query)
            || ContainsAny(item.Technologies, query)
            || ContainsAny(item.Topics, query))
        && MatchesAny(item.Href, filter.Sections, section =>
            string.Equals(GetSection(item.Href), NormalizeSection(section), StringComparison.OrdinalIgnoreCase))
        && MatchesAny(item.ContentType, filter.Types)
        && MatchesAny(item.Language, filter.Languages)
        && MatchesAny(item.Tags, filter.Tags)
        && MatchesAny(item.Technologies, filter.Technologies)
        && MatchesAny(item.Topics, filter.Topics)
        && (string.IsNullOrWhiteSpace(filter.Status)
            || string.Equals(item.Status, filter.Status, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesAny(IEnumerable<string> values, IEnumerable<string> filters) =>
        !filters.Any() || filters.Any(filter => values.Any(value => string.Equals(value, filter, StringComparison.OrdinalIgnoreCase)));

    private static bool MatchesAny(string value, IEnumerable<string> filters) =>
        !filters.Any() || filters.Any(filter => string.Equals(value, filter, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesAny<T>(T value, IEnumerable<string> filters, Func<string, bool> predicate) =>
        !filters.Any() || filters.Any(predicate);

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(IEnumerable<string> values, string query) =>
        values.Any(value => Contains(value, query));

    private static string NormalizeSection(string section) => section.ToLowerInvariant() switch
    {
        "post" or "posts" or "blog" => "posts",
        "gist" or "gists" => "gists",
        "project" or "projects" => "projects",
        _ => throw new ArgumentException("section must be posts, gists, projects, blog, gist, or project.", nameof(section)),
    };

    private static string GetSection(string href) => href.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() switch
    {
        "blog" => "posts",
        "gist" => "gists",
        "project" => "projects",
        _ => string.Empty,
    };

    private static McpContentSummary ToSummary(ContentMetadata item) => new()
    {
        Title = item.Title,
        Href = item.Href,
        Summary = item.Summary,
        PublishedAt = item.PublishedAt,
        UpdatedAt = item.UpdatedAt,
        ContentType = item.ContentType,
        Status = item.Status,
        Language = item.Language,
        Tags = item.Tags,
        Technologies = item.Technologies,
        Topics = item.Topics,
        CanonicalUrl = item.CanonicalUrl,
    };

    private static McpContentSummary ToSummary(PostModel post) => new()
    {
        Title = post.Title,
        Href = $"/{GetUrlPrefix(post.Section)}/{post.Slug}",
        Summary = post.Description,
        PublishedAt = post.PublishDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        UpdatedAt = post.UpdatedAt?.ToString("yyyy-MM-dd"),
        ContentType = post.ContentType,
        Status = post.Status,
        Language = post.Language,
        Tags = post.Tags,
        Technologies = post.Technologies,
        Topics = post.Topics,
        CanonicalUrl = post.CanonicalUrl,
    };

    private static McpContentDocument ToDocument(PostModel post, bool includeBody) => new()
    {
        Title = post.Title,
        Href = $"/{GetUrlPrefix(post.Section)}/{post.Slug}",
        Summary = post.Description,
        PublishedAt = post.PublishDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        UpdatedAt = post.UpdatedAt?.ToString("yyyy-MM-dd"),
        ContentType = post.ContentType,
        Status = post.Status,
        Language = post.Language,
        Tags = post.Tags,
        Technologies = post.Technologies,
        Topics = post.Topics,
        CanonicalUrl = post.CanonicalUrl,
        Slug = post.Slug,
        Section = post.Section,
        Image = post.Image,
        Author = post.Author,
        Content = includeBody ? post.Content : null,
        TocItems = post.TocItems,
        RelatedProjects = post.RelatedProjects,
        RelatedPosts = post.RelatedPosts,
    };

    private static string GetUrlPrefix(string section) => section switch
    {
        "posts" => "blog",
        "gists" => "gist",
        "projects" => "project",
        _ => section,
    };

    private static IReadOnlyList<McpContentSummary> ResolveReferences(
        IEnumerable<string> references,
        IEnumerable<McpContentSummary> content,
        string prefix,
        int limit) => references
            .Select(reference => content.FirstOrDefault(item =>
                item.Href.StartsWith($"/{prefix}/", StringComparison.OrdinalIgnoreCase)
                && (item.Href.EndsWith($"/{reference}", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Title, reference, StringComparison.OrdinalIgnoreCase))))
            .Where(item => item is not null)
            .Cast<McpContentSummary>()
            .Take(limit)
            .ToArray();

    private static IReadOnlyDictionary<string, int> CountValues(IEnumerable<string> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

    private static McpPage<T> Page<T>(IReadOnlyList<T> items, int limit, string? cursor)
    {
        if (limit is < 1 or > 50)
            throw new ArgumentException("limit must be between 1 and 50.", nameof(limit));

        if (!int.TryParse(cursor, out var offset) || offset < 0)
            offset = 0;

        var page = items.Skip(offset).Take(limit).ToArray();
        var nextOffset = offset + page.Length;
        return new McpPage<T>
        {
            Items = page,
            Total = items.Count,
            NextCursor = nextOffset < items.Count ? nextOffset.ToString() : null,
        };
    }
}
