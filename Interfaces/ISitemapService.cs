namespace personal_website_blazor.Interfaces;

public interface ISitemapService
{
    Task<string> GenerateSitemapXmlAsync(
        Uri baseUri,
        CancellationToken cancellationToken = default
    );
}
