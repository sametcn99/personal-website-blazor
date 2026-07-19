using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;
using personal_website_blazor.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace personal_website_blazor.Services;

public class ContentService : IContentService
{
    private readonly IWebHostEnvironment _env;
    private readonly MarkdownPipeline _pipeline;
    private readonly IDeserializer _yamlDeserializer;
    private static readonly Regex TurkishChars = new("[çğıöşüÇĞİÖŞÜ]", RegexOptions.Compiled);
    private static readonly Regex ValidSlugRegex = new(@"^[a-z0-9_-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidSections = new(StringComparer.OrdinalIgnoreCase)
        { "posts", "gists", "projects" };

    private readonly Lazy<Task<List<PostModel>>> _allPostsLazy;

    public ContentService(IWebHostEnvironment env)
    {
        _env = env;
        _pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseAdvancedExtensions()
            .Build();

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _allPostsLazy = new Lazy<Task<List<PostModel>>>(LoadAllPostsAsync);
    }

    private static bool IsValidSection(string? section) =>
        section is not null && ValidSections.Contains(section);

    private static bool IsValidSlug(string? slug) =>
        slug is not null && ValidSlugRegex.IsMatch(slug);

    /// <summary>
    /// Resolves a content file path with path traversal protection.
    /// Returns null if the section/slug is invalid or escapes the content directory.
    /// </summary>
    private string? ResolveContentPath(string section, string slug)
    {
        if (!IsValidSection(section) || !IsValidSlug(slug))
            return null;

        var contentRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "content"));
        var sectionPath = Path.GetFullPath(Path.Combine(contentRoot, section));

        if (!sectionPath.StartsWith(contentRoot, StringComparison.Ordinal))
            return null;

        var mdxPath = Path.Combine(sectionPath, $"{slug}.mdx");
        if (File.Exists(mdxPath))
            return mdxPath;

        var mdPath = Path.Combine(sectionPath, $"{slug}.md");
        if (File.Exists(mdPath))
            return mdPath;

        return null;
    }

    public async Task<PostModel?> GetPostAsync(string section, string slug)
    {
        var path = ResolveContentPath(section, slug);
        if (path is null)
            return null;

        try
        {
            var content = await File.ReadAllTextAsync(path);
            var document = Markdown.Parse(content, _pipeline);

            var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
            var post = new PostModel { Slug = slug, Section = section };

            if (yamlBlock != null)
            {
                var yaml = content.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                yaml = yaml.Replace("---", "").Trim();

                try
                {
                    var metadata = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
                    if (metadata.TryGetValue("title", out var title))
                        post.Title = title.ToString()!;
                    if (metadata.TryGetValue("description", out var desc))
                        post.Description = desc.ToString()!;
                    if (metadata.TryGetValue("summary", out var summary))
                        post.Description = summary.ToString()!;
                    if (
                        metadata.TryGetValue("publishDate", out var pd)
                        && DateTime.TryParse(pd.ToString(), out var date)
                    )
                        post.PublishDate = date;
                    if (
                        metadata.TryGetValue("publishedAt", out var pa)
                        && DateTime.TryParse(pa.ToString(), out var date2)
                    )
                        post.PublishDate = date2;
                    if (
                        metadata.TryGetValue("updatedAt", out var ua)
                        && DateTime.TryParse(ua.ToString(), out var updatedDate)
                    )
                        post.UpdatedAt = updatedDate;
                    if (metadata.TryGetValue("image", out var img))
                        post.Image = img.ToString();
                    if (metadata.TryGetValue("author", out var author))
                        post.Author = author.ToString();
                    if (metadata.TryGetValue("tags", out var tags))
                    {
                        if (tags is List<object> tagsList)
                            post.Tags = tagsList.Select(x => x.ToString()!).ToArray();
                    }
                    if (metadata.TryGetValue("language", out var lang))
                        post.Language = lang.ToString()!;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing YAML for {slug}: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(post.Language) || post.Language == "en")
            {
                if (TurkishChars.IsMatch(content))
                    post.Language = "tr";
            }

            post.Content = Markdown.ToHtml(content, _pipeline);
            post.SearchableText = HtmlUtility.StripHtml(post.Content);
            post.TocItems = HtmlUtility.ExtractHeadings(post.Content).ToArray();
            return post;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing post {section}/{slug}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<PostModel>> GetPostsAsync(string section)
    {
        if (!IsValidSection(section))
            return new List<PostModel>();

        var dirPath = Path.Combine(_env.ContentRootPath, "content", section);
        if (!Directory.Exists(dirPath))
            return new List<PostModel>();

        var files = Directory.GetFiles(dirPath, "*.md*");
        var posts = new List<PostModel>();

        foreach (var file in files)
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            if (!IsValidSlug(slug))
                continue;

            var post = await GetPostAsync(section, slug);
            if (post != null)
                posts.Add(post);
        }

        return posts.OrderByDescending(p => p.PublishDate).ToList();
    }

    /// <summary>
    /// Loads only YAML front matter metadata for a section — skips full Markdown → HTML rendering.
    /// Useful for list views that don't need Content/SearchableText/TocItems, avoiding oversized
    /// SignalR payloads and memory pressure with large content directories.
    /// </summary>
    public async Task<List<PostModel>> GetPostMetadataListAsync(string section)
    {
        if (!IsValidSection(section))
            return new List<PostModel>();

        var dirPath = Path.Combine(_env.ContentRootPath, "content", section);
        if (!Directory.Exists(dirPath))
            return new List<PostModel>();

        var files = Directory.GetFiles(dirPath, "*.md*");
        var posts = new List<PostModel>();

        foreach (var file in files)
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            if (!IsValidSlug(slug))
                continue;

            try
            {
                var content = await File.ReadAllTextAsync(file);
                var document = Markdown.Parse(content, _pipeline);

                var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
                var post = new PostModel { Slug = slug, Section = section };

                if (yamlBlock != null)
                {
                    var yaml = content.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                    yaml = yaml.Replace("---", "").Trim();

                    try
                    {
                        var metadata = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
                        if (metadata.TryGetValue("title", out var title))
                            post.Title = title.ToString()!;
                        if (metadata.TryGetValue("description", out var desc))
                            post.Description = desc.ToString()!;
                        if (metadata.TryGetValue("summary", out var summary))
                            post.Description = summary.ToString()!;
                        if (metadata.TryGetValue("publishDate", out var pd) && DateTime.TryParse(pd.ToString(), out var date))
                            post.PublishDate = date;
                        if (metadata.TryGetValue("publishedAt", out var pa) && DateTime.TryParse(pa.ToString(), out var date2))
                            post.PublishDate = date2;
                        if (metadata.TryGetValue("updatedAt", out var ua) && DateTime.TryParse(ua.ToString(), out var updatedDate))
                            post.UpdatedAt = updatedDate;
                        if (metadata.TryGetValue("image", out var img))
                            post.Image = img.ToString();
                        if (metadata.TryGetValue("author", out var author))
                            post.Author = author.ToString();
                        if (metadata.TryGetValue("tags", out var tags) && tags is List<object> tagsList)
                            post.Tags = tagsList.Select(x => x.ToString()!).ToArray();
                        if (metadata.TryGetValue("language", out var lang))
                            post.Language = lang.ToString()!;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing YAML metadata for {section}/{slug}: {ex.Message}");
                    }
                }

                posts.Add(post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading metadata for {section}/{slug}: {ex.Message}");
            }
        }

        return posts.OrderByDescending(p => p.PublishDate).ToList();
    }

    public async Task<List<ContentMetadata>> GetAllContentsAsync()
    {
        var result = new List<ContentMetadata>();

        var sections = new[] { ("posts", "blog"), ("gists", "gist"), ("projects", "project") };
        foreach (var (fsSection, urlPrefix) in sections)
        {
            var posts = await GetPostsAsync(fsSection);
            result.AddRange(
                posts.Select(p => new ContentMetadata
                {
                    Title = p.Title,
                    Href = $"/{urlPrefix}/{p.Slug}",
                    PublishedAt = p.PublishDate?.ToString("yyyy-MM-dd") ?? "",
                    UpdatedAt = p.UpdatedAt?.ToString("yyyy-MM-dd"),
                    Summary = p.Description ?? string.Empty,
                    SearchableText = p.SearchableText,
                    Tags = p.Tags,
                    Language = p.Language,
                })
            );
        }

        return result
            .OrderByDescending(item => GetMetadataDate(item.UpdatedAt) ?? GetMetadataDate(item.PublishedAt))
            .ToList();
    }

    public async Task<List<SearchResult>> SearchAsync(string query, string? section = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResult>();

        var allPosts = await GetAllPostsCached();
        var q = query.Trim();

        var sections = section is not null
            ? new[] { (section, GetUrlPrefix(section)) }
            : new[] { ("posts", "blog"), ("gists", "gist"), ("projects", "project") };

        var results = new List<SearchResult>();

        foreach (var (fsSection, urlPrefix) in sections)
        {
            var sectionPosts = allPosts.Where(p => p.Section == fsSection);

            foreach (var post in sectionPosts)
            {
                var matchTitle = post.Title.Contains(q, StringComparison.OrdinalIgnoreCase);
                var matchDesc = post.Description is not null && post.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
                var matchTags = post.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase));
                var matchContent = post.SearchableText is not null && post.SearchableText.Contains(q, StringComparison.OrdinalIgnoreCase);

                if (!matchTitle && !matchDesc && !matchTags && !matchContent)
                    continue;

                var snippet = matchContent
                    ? HtmlUtility.GetSnippet(post.SearchableText, q)
                    : matchTags
                        ? $"Tags: {string.Join(", ", post.Tags)}"
                        : post.Description;

                var typeLabel = urlPrefix switch
                {
                    "blog" => "Blog",
                    "gist" => "Gist",
                    "project" => "Project",
                    _ => "Article",
                };

                results.Add(new SearchResult
                {
                    Title = post.Title,
                    Summary = post.Description ?? string.Empty,
                    Href = $"/{urlPrefix}/{post.Slug}",
                    TypeLabel = typeLabel,
                    PublishedAt = post.PublishDate?.ToString("yyyy-MM-dd"),
                    UpdatedAt = post.UpdatedAt?.ToString("yyyy-MM-dd"),
                    MatchSnippet = snippet ?? string.Empty,
                    Tags = post.Tags,
                });
            }
        }

        return results
            .OrderByDescending(r => r.Title.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenByDescending(r => r.MatchSnippet?.Length ?? 0)
            .ToList();
    }

    public Task<List<PostModel>> GetAllPostsCached() => _allPostsLazy.Value;

    private async Task<List<PostModel>> LoadAllPostsAsync()
    {
        var allPosts = new List<PostModel>();
        var sections = new[] { "posts", "gists", "projects" };

        foreach (var section in sections)
        {
            var dirPath = Path.Combine(_env.ContentRootPath, "content", section);
            if (!Directory.Exists(dirPath))
                continue;

            var files = Directory.GetFiles(dirPath, "*.md*");
            foreach (var file in files)
            {
                var slug = Path.GetFileNameWithoutExtension(file);
                if (!IsValidSlug(slug))
                    continue;

                var content = await File.ReadAllTextAsync(file);
                var document = Markdown.Parse(content, _pipeline);
                var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
                var post = new PostModel { Slug = slug, Section = section };

                if (yamlBlock != null)
                {
                    var yaml = content.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                    yaml = yaml.Replace("---", "").Trim();
                    try
                    {
                        var metadata = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
                        if (metadata.TryGetValue("title", out var title))
                            post.Title = title.ToString()!;
                        if (metadata.TryGetValue("description", out var desc))
                            post.Description = desc.ToString()!;
                        if (metadata.TryGetValue("summary", out var summary))
                            post.Description = summary.ToString()!;
                        if (metadata.TryGetValue("publishDate", out var pd) && DateTime.TryParse(pd.ToString(), out var date))
                            post.PublishDate = date;
                        if (metadata.TryGetValue("publishedAt", out var pa) && DateTime.TryParse(pa.ToString(), out var date2))
                            post.PublishDate = date2;
                        if (metadata.TryGetValue("updatedAt", out var ua) && DateTime.TryParse(ua.ToString(), out var updatedDate))
                            post.UpdatedAt = updatedDate;
                        if (metadata.TryGetValue("image", out var img))
                            post.Image = img.ToString();
                        if (metadata.TryGetValue("author", out var author))
                            post.Author = author.ToString();
                        if (metadata.TryGetValue("tags", out var tags) && tags is List<object> tagsList)
                            post.Tags = tagsList.Select(x => x.ToString()!).ToArray();
                        if (metadata.TryGetValue("language", out var lang))
                            post.Language = lang.ToString()!;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing YAML for {slug}: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(post.Language) || post.Language == "en")
                {
                    if (TurkishChars.IsMatch(content))
                        post.Language = "tr";
                }

                post.Content = Markdown.ToHtml(content, _pipeline);
                post.SearchableText = HtmlUtility.StripHtml(post.Content);
                post.TocItems = HtmlUtility.ExtractHeadings(post.Content).ToArray();
                allPosts.Add(post);
            }
        }

        return allPosts;
    }

    private static string GetUrlPrefix(string section) => section switch
    {
        "posts" => "blog",
        "gists" => "gist",
        "projects" => "project",
        _ => section,
    };

    private static string? GetMetadataDate(string? date) =>
        !string.IsNullOrWhiteSpace(date) ? date : null;
}
