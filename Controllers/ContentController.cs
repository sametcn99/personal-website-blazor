using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;
using Markdig;

namespace personal_website_blazor.Controllers;

[ApiController]
[Route("api")]
public class ContentController : ControllerBase
{
    private static readonly Regex ValidSlugRegex = new(@"^[a-z0-9_-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> ValidSections = new(StringComparer.OrdinalIgnoreCase)
        { "posts", "gists", "projects", "links" };

    private static readonly MarkdownPipeline CvPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly IContentService _contentService;
    private readonly IGitHubService _gitHubService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;

    public ContentController(
        IContentService contentService,
        IGitHubService gitHubService,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env)
    {
        _contentService = contentService;
        _gitHubService = gitHubService;
        _httpClientFactory = httpClientFactory;
        _env = env;
    }

    [HttpGet("content/all")]
    public async Task<ActionResult<IEnumerable<ContentMetadata>>> GetAll()
    {
        var contents = await _contentService.GetAllContentsAsync();
        return Ok(contents);
    }

    [HttpGet("content/search")]
    public async Task<ActionResult<IEnumerable<SearchResult>>> Search([FromQuery] string? q, [FromQuery] string? section)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 200)
            return Ok(new List<SearchResult>());

        var results = await _contentService.SearchAsync(q, section);
        return Ok(results);
    }

    [HttpGet("content/{section}")]
    public async Task<ActionResult<IEnumerable<PostModel>>> GetPosts(string section)
    {
        if (!ValidSections.Contains(section))
            return NotFound();

        var posts = await _contentService.GetPostsAsync(section);
        return Ok(posts);
    }

    [HttpGet("content/{section}/{slug}")]
    public async Task<ActionResult<PostModel>> GetPost(string section, string slug)
    {
        if (!ValidSections.Contains(section) || !ValidSlugRegex.IsMatch(slug))
            return NotFound();

        var post = await _contentService.GetPostAsync(section, slug);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpGet("content/cv")]
    public async Task<ActionResult> GetCv()
    {
        var path = Path.Combine(_env.ContentRootPath, "content", "cv.mdx");
        if (!System.IO.File.Exists(path))
            return NotFound();

        var markdown = await System.IO.File.ReadAllTextAsync(path);
        return Ok(new { html = Markdown.ToHtml(markdown, CvPipeline) });
    }

    [HttpGet("readme")]
    public async Task<ActionResult> GetReadme()
    {
        var client = _httpClientFactory.CreateClient("GitHub");
        var markdown = await client.GetStringAsync(
            "https://raw.githubusercontent.com/sametcn99/sametcn99/main/README.md");
        return Ok(new { html = Markdown.ToHtml(markdown, CvPipeline) });
    }

    [HttpGet("repos")]
    public async Task<ActionResult<IEnumerable<GitHubRepository>>> GetRepos()
    {
        var repos = await _gitHubService.GetUserRepositoriesAsync("sametcn99");
        return Ok(repos);
    }
}
