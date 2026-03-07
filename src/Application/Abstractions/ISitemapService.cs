namespace personal_website_blazor.Application.Abstractions;

public interface ISitemapService
{
    Task<string> GenerateSitemapXmlAsync(
        Uri baseUri,
        CancellationToken cancellationToken = default
    );
}
