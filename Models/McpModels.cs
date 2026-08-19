namespace personal_website_blazor.Models;

public sealed class McpPage<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Total { get; init; }
    public string? NextCursor { get; init; }
}

public class McpContentSummary
{
    public string Title { get; init; } = string.Empty;
    public string Href { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string PublishedAt { get; init; } = string.Empty;
    public string? UpdatedAt { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string Status { get; init; } = "published";
    public string Language { get; init; } = "en";
    public string[] Tags { get; init; } = Array.Empty<string>();
    public string[] Technologies { get; init; } = Array.Empty<string>();
    public string[] Topics { get; init; } = Array.Empty<string>();
    public string? CanonicalUrl { get; init; }
}

public sealed class McpContentDocument : McpContentSummary
{
    public string Slug { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string? Image { get; init; }
    public string? Author { get; init; }
    public string? Content { get; init; }
    public TocItem[] TocItems { get; init; } = Array.Empty<TocItem>();
    public string[] RelatedProjects { get; init; } = Array.Empty<string>();
    public string[] RelatedPosts { get; init; } = Array.Empty<string>();
    public McpRelatedContentResult? RelatedContent { get; set; }
}

public sealed class McpSearchFilter
{
    public string Query { get; init; } = string.Empty;
    public string[] Sections { get; init; } = Array.Empty<string>();
    public string[] Types { get; init; } = Array.Empty<string>();
    public string[] Languages { get; init; } = Array.Empty<string>();
    public string[] Tags { get; init; } = Array.Empty<string>();
    public string[] Technologies { get; init; } = Array.Empty<string>();
    public string[] Topics { get; init; } = Array.Empty<string>();
    public string? Status { get; init; }
    public int Limit { get; init; } = 20;
    public string? Cursor { get; init; }
}

public sealed class McpProjectFilter
{
    public string? Query { get; init; }
    public string[] Tags { get; init; } = Array.Empty<string>();
    public string[] Technologies { get; init; } = Array.Empty<string>();
    public string[] Topics { get; init; } = Array.Empty<string>();
    public string? Language { get; init; }
    public int Limit { get; init; } = 20;
    public string? Cursor { get; init; }
}

public sealed class McpSkillsResult
{
    public string[] Skills { get; init; } = Array.Empty<string>();
    public string[] AreasOfInterest { get; init; } = Array.Empty<string>();
    public string[] Languages { get; init; } = Array.Empty<string>();
    public string[] PublicNotes { get; init; } = Array.Empty<string>();
    public string LastUpdated { get; init; } = string.Empty;
}

public sealed class McpTaxonomyResult
{
    public IReadOnlyDictionary<string, int> Tags { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> Technologies { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> Topics { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> Types { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> Languages { get; init; } = new Dictionary<string, int>();
}

public sealed class McpRelatedContentResult
{
    public McpContentSummary Source { get; init; } = new();
    public IReadOnlyList<McpContentSummary> Projects { get; init; } = Array.Empty<McpContentSummary>();
    public IReadOnlyList<McpContentSummary> Posts { get; init; } = Array.Empty<McpContentSummary>();
}
