using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Microsoft.AspNetCore.Hosting;
using personal_website_blazor.Application.Abstractions;
using personal_website_blazor.Domain.Entities;
using personal_website_blazor.Domain.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace personal_website_blazor.Infrastructure.Content;

public class ContentService : IContentService
{
    private readonly IWebHostEnvironment _env;
    private readonly MarkdownPipeline _pipeline;
    private readonly IDeserializer _yamlDeserializer;
    private static readonly Regex TurkishChars = new("[çğıöşüÇĞİÖŞÜ]", RegexOptions.Compiled);

    private List<PostModel>? _allPostsCache;
    private readonly object _cacheLock = new();

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
    }

    public async Task<PostModel?> GetPostAsync(string section, string slug)
    {
        var path = Path.Combine(_env.ContentRootPath, "content", section, $"{slug}.mdx");
        if (!File.Exists(path))
        {
            path = Path.Combine(_env.ContentRootPath, "content", section, $"{slug}.md");
            if (!File.Exists(path))
                return null;
        }

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

    public async Task<List<PostModel>> GetPostsAsync(string section)
    {
        var dirPath = Path.Combine(_env.ContentRootPath, "content", section);
        if (!Directory.Exists(dirPath))
            return new List<PostModel>();

        var files = Directory.GetFiles(dirPath, "*.md*");
        var posts = new List<PostModel>();

        foreach (var file in files)
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            var post = await GetPostAsync(section, slug);
            if (post != null)
                posts.Add(post);
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
                    Summary = p.Description,
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
                var matchDesc = post.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
                var matchTags = post.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase));
                var matchContent = post.SearchableText.Contains(q, StringComparison.OrdinalIgnoreCase);

                if (!matchTitle && !matchDesc && !matchTags && !matchContent)
                    continue;

                var snippet = matchContent
                    ? HtmlUtility.GetSnippet(post.SearchableText, q)
                    : matchTags
                        ? $"Tags: {string.Join(", ", post.Tags)}"
                        : post.Description;

                results.Add(new SearchResult
                {
                    Title = post.Title,
                    Summary = post.Description,
                    Href = $"/{urlPrefix}/{post.Slug}",
                    TypeLabel = urlPrefix[..1].ToUpperInvariant() + urlPrefix[1..],
                    PublishedAt = post.PublishDate?.ToString("yyyy-MM-dd"),
                    UpdatedAt = post.UpdatedAt?.ToString("yyyy-MM-dd"),
                    MatchSnippet = snippet,
                    Tags = post.Tags,
                });
            }
        }

        return results
            .OrderByDescending(r => r.UpdatedAt ?? r.PublishedAt)
            .Take(20)
            .ToList();
    }

    private async Task<List<PostModel>> GetAllPostsCached()
    {
        lock (_cacheLock)
        {
            if (_allPostsCache != null)
                return _allPostsCache;
        }

        var sections = new[] { "posts", "gists", "projects" };
        var allPosts = new List<PostModel>();

        foreach (var section in sections)
        {
            allPosts.AddRange(await GetPostsAsync(section));
        }

        lock (_cacheLock)
        {
            _allPostsCache = allPosts;
        }

        return allPosts;
    }

    private static string GetUrlPrefix(string section) =>
        section switch
        {
            "posts" => "blog",
            "gists" => "gist",
            "projects" => "project",
            _ => section,
        };

    private static DateTime? GetMetadataDate(string? value)
        => DateTime.TryParse(value, out var parsed) ? parsed : null;
}
