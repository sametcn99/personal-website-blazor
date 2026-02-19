using Microsoft.Extensions.Options;
using personal_website_blazor.Core.Application.Abstractions;
using personal_website_blazor.Core.Application.Configuration;

namespace personal_website_blazor.Presentation.Startup.Internal.Endpoints;

internal static class SyndicationEndpointExtensions
{
    internal static WebApplication MapSyndicationEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/rss.xml",
            async (
                HttpContext context,
                IRssFeedService feedService,
                IOptions<CachePolicyOptions> cacheOptions
            ) =>
            {
                var request = context.Request;
                var baseUri = new Uri($"{request.Scheme}://{request.Host}");

                var xml = await feedService.BuildFeedAsync(baseUri);

                context.Response.Headers.CacheControl =
                    $"public, max-age={cacheOptions.Value.RssMaxAgeSeconds}";
                return Results.Content(xml, "application/rss+xml");
            }
        );

        app.MapGet(
            "/sitemap.xml",
            async (
                HttpContext context,
                ISitemapService sitemapService,
                IOptions<CachePolicyOptions> cacheOptions
            ) =>
            {
                var request = context.Request;
                var baseUri = new Uri($"{request.Scheme}://{request.Host}");

                var xml = await sitemapService.GenerateSitemapXmlAsync(baseUri);

                context.Response.Headers.CacheControl =
                    $"public, max-age={cacheOptions.Value.SitemapMaxAgeSeconds}";
                return Results.Content(xml, "application/xml");
            }
        );

        // JSON Feed (https://www.jsonfeed.org/version/1.1/)
        app.MapGet(
            "/feed.json",
            async (
                HttpContext context,
                IContentService contentService,
                IOptions<CachePolicyOptions> cacheOptions
            ) =>
            {
                var request = context.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var allContents = await contentService.GetAllContentsAsync();

                var items = allContents.Select(c => new
                {
                    id = $"{baseUrl}{c.Href}",
                    url = $"{baseUrl}{c.Href}",
                    title = c.Title,
                    summary = c.Summary,
                    date_published = c.PublishedAt,
                    tags = c.Tags,
                });

                var feed = new
                {
                    version = "https://jsonfeed.org/version/1.1",
                    title = "Samet Can Cıncık",
                    home_page_url = baseUrl,
                    feed_url = $"{baseUrl}/feed.json",
                    items,
                };

                context.Response.Headers.CacheControl =
                    $"public, max-age={cacheOptions.Value.FeedJsonMaxAgeSeconds}";
                return Results.Json(feed);
            }
        );

        return app;
    }
}
