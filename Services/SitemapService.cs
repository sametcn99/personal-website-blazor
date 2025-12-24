// Dynamically generates a sitemap XML for the website.
using System.Xml.Linq;

namespace personal_website_blazor.Services;

public interface ISitemapService
{
    Task<string> GenerateSitemapXmlAsync(
        Uri baseUri,
        CancellationToken cancellationToken = default
    );
}

public class SitemapService : ISitemapService
{
    private readonly IContentService _contentService;

    public SitemapService(IContentService contentService)
    {
        _contentService = contentService;
    }

    public async Task<string> GenerateSitemapXmlAsync(
        Uri baseUri,
        CancellationToken cancellationToken = default
    )
    {
        var staticPages = new List<(string path, string changefreq, string priority)>
        {
            ("", "monthly", "1.0"),
            ("about", "monthly", "0.8"),
            ("blog", "weekly", "0.9"),
            ("projects", "weekly", "0.9"),
            ("contact", "monthly", "0.8"),
        };

        var urlElements = new List<XElement>();

        foreach (var (path, changefreq, priority) in staticPages)
        {
            var loc = new Uri(baseUri, path).ToString();
            var url = new XElement(
                "url",
                new XElement("loc", loc),
                new XElement("changefreq", changefreq),
                new XElement("priority", priority)
            );
            urlElements.Add(url);
        }

        var sections = new[] { "posts", "gists", "projects" };

        foreach (var section in sections)
        {
            var posts = await _contentService.GetPostsAsync(section);

            foreach (var post in posts)
            {
                var loc = new Uri(baseUri, $"{section}/{post.Slug}").ToString();
                var url = new XElement("url", new XElement("loc", loc));

                if (post.PublishDate.HasValue)
                {
                    url.Add(new XElement("lastmod", post.PublishDate.Value.ToString("yyyy-MM-dd")));
                }

                // set sensible defaults; posts slightly higher priority
                url.Add(new XElement("changefreq", "monthly"));
                url.Add(new XElement("priority", section == "posts" ? "0.8" : "0.7"));

                urlElements.Add(url);
            }
        }

        var sitemap = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(
                "urlset",
                new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                urlElements
            )
        );

        return sitemap.ToString(SaveOptions.DisableFormatting);
    }
}
