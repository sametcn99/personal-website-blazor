using Markdig;
using Markdig.Syntax;
using Markdig.Extensions.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace personal_website_blazor.Services;

public class PostModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? PublishDate { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Content { get; set; } = string.Empty; // HTML content
    public string Slug { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty; // posts, projects, gists
}

public interface IMarkdownService
{
    Task<PostModel?> GetPostAsync(string section, string slug);
    Task<List<PostModel>> GetPostsAsync(string section);
}

public class MarkdownService : IMarkdownService
{
    private readonly IWebHostEnvironment _env;
    private readonly MarkdownPipeline _pipeline;
    private readonly IDeserializer _yamlDeserializer;

    public MarkdownService(IWebHostEnvironment env)
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
            // Try .md extension as fallback
            path = Path.Combine(_env.ContentRootPath, "content", section, $"{slug}.md");
            if (!File.Exists(path)) return null;
        }

        var content = await File.ReadAllTextAsync(path);
        var document = Markdown.Parse(content, _pipeline);

        // Extract FrontMatter
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        var post = new PostModel { Slug = slug, Section = section };

        if (yamlBlock != null)
        {
            var yaml = content.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
            // Remove --- fences
            yaml = yaml.Replace("---", "").Trim();

            try
            {
                var metadata = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
                if (metadata.ContainsKey("title")) post.Title = metadata["title"].ToString()!;
                if (metadata.ContainsKey("description")) post.Description = metadata["description"].ToString()!;
                if (metadata.ContainsKey("publishDate") && DateTime.TryParse(metadata["publishDate"].ToString(), out var date)) post.PublishDate = date;
                if (metadata.ContainsKey("tags"))
                {
                    // Handle tags which could be a list
                    var tagsList = metadata["tags"] as List<object>;
                    if (tagsList != null)
                    {
                        post.Tags = tagsList.Select(x => x.ToString()!).ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing YAML for {slug}: {ex.Message}");
            }
        }

        // Render HTML (excluding frontmatter? Markdig usually handles this but we might want to strip it if it shows up)
        // Note: Markdig's UseYamlFrontMatter hides the frontmatter from HTML output automatically.
        post.Content = Markdown.ToHtml(content, _pipeline);

        return post;
    }

    public async Task<List<PostModel>> GetPostsAsync(string section)
    {
        var dirPath = Path.Combine(_env.ContentRootPath, "content", section);
        if (!Directory.Exists(dirPath)) return new List<PostModel>();

        var files = Directory.GetFiles(dirPath, "*.md*"); // mdx or md
        var posts = new List<PostModel>();

        foreach (var file in files)
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            var post = await GetPostAsync(section, slug);
            if (post != null)
            {
                posts.Add(post);
            }
        }

        return posts.OrderByDescending(p => p.PublishDate).ToList();
    }
}
