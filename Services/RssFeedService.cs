using System.Xml.Linq;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

public class RssFeedService : IRssFeedService
{
    private readonly IContentService _contentService;

    public RssFeedService(IContentService contentService)
    {
        _contentService = contentService;
    }

    public async Task<string> BuildFeedAsync(Uri baseUri)
    {
        var contents = await _contentService.GetAllContentsAsync();

        var lastBuildDate = contents
            .Select(content => ParseDate(content.UpdatedAt) ?? ParseDate(content.PublishedAt))
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .DefaultIfEmpty(DateTimeOffset.UtcNow)
            .Max();
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
                    new XElement("lastBuildDate", lastBuildDate.ToString("r")),
                    contents.Select(content => BuildItem(content, baseUri, lastBuildDate))
                )
            )
        );

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildItem(ContentMetadata content, Uri baseUri, DateTimeOffset fallbackDate)
    {
        var itemLink = new Uri(baseUri, content.Href).ToString();
        var publishDate = (ParseDate(content.PublishedAt) ?? fallbackDate).ToUniversalTime().ToString("r");

        return new XElement(
            "item",
            new XElement("title", content.Title),
            new XElement("link", itemLink),
            new XElement("guid", itemLink),
            new XElement("pubDate", publishDate),
            new XElement("description", new XCData(content.Summary))
        );
    }

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, out var date) ? date : null;
}
