using System.Xml.Linq;

namespace personal_website_blazor.Services;

public interface IRssFeedService
{
    Task<string> BuildFeedAsync(Uri baseUri);
}

public class RssFeedService : IRssFeedService
{
    private readonly IMarkdownService _markdownService;

    public RssFeedService(IMarkdownService markdownService)
    {
        _markdownService = markdownService;
    }

    public async Task<string> BuildFeedAsync(Uri baseUri)
    {
        var posts = await _markdownService.GetPostsAsync("posts");

        var now = DateTimeOffset.UtcNow;
        var channelTitle = "Samet Can Cıncık - Blog";
        var channelDescription = "Recent posts and updates";

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(
                "rss",
                new XAttribute("version", "2.0"),
                new XElement(
                    "channel",
                    new XElement("title", channelTitle),
                    new XElement("link", baseUri.ToString().TrimEnd('/')),
                    new XElement("description", channelDescription),
                    new XElement("lastBuildDate", now.ToString("r")),
                    posts.Select(post => BuildItem(post, baseUri, now))
                )
            )
        );

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildItem(PostModel post, Uri baseUri, DateTimeOffset fallbackDate)
    {
        var itemLink = new Uri(baseUri, $"/blog/{post.Slug}").ToString();
        var publishDate = (post.PublishDate ?? fallbackDate).ToUniversalTime().ToString("r");

        return new XElement(
            "item",
            new XElement("title", post.Title),
            new XElement("link", itemLink),
            new XElement("guid", itemLink),
            new XElement("pubDate", publishDate),
            new XElement("description", new XCData(post.Description))
        );
    }
}
