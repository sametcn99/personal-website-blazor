// Dynamically generates a sitemap XML for the website.
using System.Xml.Linq;
using personal_website_blazor.Core.Application.Abstractions;
using personal_website_blazor.Core.Application.Configuration;

namespace personal_website_blazor.Infrastructure.Seo;

public class SitemapService : ISitemapService
{
    private readonly IContentService _contentService;
    private readonly IGitHubService _gitHubService;

    public SitemapService(IContentService contentService, IGitHubService gitHubService)
    {
        _contentService = contentService;
        _gitHubService = gitHubService;
    }

    public async Task<string> GenerateSitemapXmlAsync(
        Uri baseUri,
        CancellationToken cancellationToken = default
    )
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var staticPages = new List<(string path, string changefreq, string priority)>
        {
            ("", "monthly", "1.0"),
            ("blog", "weekly", "0.9"),
            ("project", "weekly", "0.8"),
            ("gist", "monthly", "0.7"),
            ("rss", "monthly", "0.4"),
            ("support", "yearly", "0.3"),
            ("readme", "weekly", "0.5"),
            ("cv", "monthly", "0.5"),
            ("writer", "weekly", "0.4"),
            ("link", "weekly", "0.5"),
            ("repo", "weekly", "0.6"),
            ("privacy-policy", "yearly", "0.2"),
        };

        var urlElements = new List<XElement>();

        foreach (var (path, changefreq, priority) in staticPages)
        {
            var loc = new Uri(baseUri, path).ToString();
            var url = new XElement(
                ns + "url",
                new XElement(ns + "loc", loc),
                new XElement(ns + "changefreq", changefreq),
                new XElement(ns + "priority", priority)
            );
            urlElements.Add(url);
        }

        var sections = new[]
        {
            (fsSection: "posts", routePrefix: "blog", priority: "0.6"),
            (fsSection: "gists", routePrefix: "gist", priority: "0.6"),
            (fsSection: "projects", routePrefix: "project", priority: "0.6"),
        };

        foreach (var section in sections)
        {
            var posts = await _contentService.GetPostsAsync(section.fsSection);

            foreach (var post in posts)
            {
                var loc = new Uri(baseUri, $"{section.routePrefix}/{post.Slug}").ToString();
                var url = new XElement(ns + "url", new XElement(ns + "loc", loc));

                if (post.PublishDate.HasValue)
                {
                    url.Add(
                        new XElement(ns + "lastmod", post.PublishDate.Value.ToString("yyyy-MM-dd"))
                    );
                }

                url.Add(new XElement(ns + "changefreq", "monthly"));
                url.Add(new XElement(ns + "priority", section.priority));

                urlElements.Add(url);
            }
        }

        foreach (var link in SocialData.Links)
        {
            foreach (var slug in link.Type)
            {
                var loc = new Uri(baseUri, $"link/{slug}").ToString();
                var url = new XElement(
                    ns + "url",
                    new XElement(ns + "loc", loc),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.5")
                );

                urlElements.Add(url);
            }
        }

        try
        {
            var repos = await _gitHubService.GetUserRepositoriesAsync("sametcn99", 100);

            foreach (var repo in repos)
            {
                if (string.IsNullOrWhiteSpace(repo.Name))
                {
                    continue;
                }

                var loc = new Uri(baseUri, $"repo/{repo.Name}").ToString();
                var url = new XElement(
                    ns + "url",
                    new XElement(ns + "loc", loc),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.6")
                );

                if (repo.UpdatedAt.HasValue)
                {
                    url.Add(
                        new XElement(
                            ns + "lastmod",
                            repo.UpdatedAt.Value.UtcDateTime.ToString("yyyy-MM-dd")
                        )
                    );
                }

                urlElements.Add(url);
            }
        }
        catch { }

        var sitemap = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset", urlElements)
        );

        return sitemap.ToString(SaveOptions.DisableFormatting);
    }
}
