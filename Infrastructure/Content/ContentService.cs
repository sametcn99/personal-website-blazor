using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using personal_website_blazor.Core.Application.Abstractions;
using personal_website_blazor.Core.Domain.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace personal_website_blazor.Infrastructure.Content;

public class ContentService : IContentService
{
    private readonly IWebHostEnvironment _env;
    private readonly MarkdownPipeline _pipeline;
    private readonly IDeserializer _yamlDeserializer;
    private static readonly Regex TurkishChars = new("[çğıöşüÇĞİÖŞÜ]", RegexOptions.Compiled);

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

        // Auto-detect language if not set
        if (string.IsNullOrEmpty(post.Language) || post.Language == "en")
        {
            if (TurkishChars.IsMatch(content))
                post.Language = "tr";
        }

        post.Content = Markdown.ToHtml(content, _pipeline);
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
                    Summary = p.Description,
                    Tags = p.Tags,
                    Language = p.Language,
                })
            );
        }

        return result;
    }
}
