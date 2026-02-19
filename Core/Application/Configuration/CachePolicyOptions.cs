namespace personal_website_blazor.Core.Application.Configuration;

public sealed class CachePolicyOptions
{
    public const string SectionName = "CachePolicy";

    public int StaticAssetsMaxAgeSeconds { get; set; } = 3600;
    public int RssMaxAgeSeconds { get; set; } = 1800;
    public int SitemapMaxAgeSeconds { get; set; } = 86400;
    public int FeedJsonMaxAgeSeconds { get; set; } = 1800;
    public int ManifestMaxAgeSeconds { get; set; } = 600;
}
