using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Controllers;

[ApiController]
[Route("/")]
public class SyndicationController : ControllerBase
{
    private readonly IRssFeedService _feedService;
    private readonly ISitemapService _sitemapService;
    private readonly IContentService _contentService;
    private readonly IOptions<CachePolicyOptions> _cacheOptions;

    public SyndicationController(
        IRssFeedService feedService,
        ISitemapService sitemapService,
        IContentService contentService,
        IOptions<CachePolicyOptions> cacheOptions)
    {
        _feedService = feedService;
        _sitemapService = sitemapService;
        _contentService = contentService;
        _cacheOptions = cacheOptions;
    }

    [HttpGet("rss.xml")]
    public async Task<ActionResult> GetRss()
    {
        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var xml = await _feedService.BuildFeedAsync(baseUri);
        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.RssMaxAgeSeconds}";
        return Content(xml, "application/rss+xml");
    }

    [HttpGet("sitemap.xml")]
    public async Task<ActionResult> GetSitemap()
    {
        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var xml = await _sitemapService.GenerateSitemapXmlAsync(baseUri);
        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.SitemapMaxAgeSeconds}";
        return Content(xml, "application/xml");
    }

    [HttpGet("feed.json")]
    public async Task<ActionResult> GetFeedJson()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var allContents = await _contentService.GetAllContentsAsync();

        var items = allContents.Select(c => new
        {
            id = $"{baseUrl}{c.Href}",
            url = $"{baseUrl}{c.Href}",
            title = c.Title,
            summary = c.Summary,
            date_published = c.PublishedAt,
            date_modified = c.UpdatedAt,
            tags = c.Tags,
        });

        var feed = new
        {
            version = "https://jsonfeed.org/version/1.1",
            title = "Samet Can Cıncık | Blog & Projects Feed",
            home_page_url = baseUrl,
            description = "Latest posts, gists, and projects from sametcc.me",
            items = items,
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.FeedJsonMaxAgeSeconds}";
        return Ok(feed);
    }
}
