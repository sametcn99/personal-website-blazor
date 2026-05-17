namespace personal_website_blazor.Interfaces;

public interface IRssFeedService
{
    Task<string> BuildFeedAsync(Uri baseUri);
}
