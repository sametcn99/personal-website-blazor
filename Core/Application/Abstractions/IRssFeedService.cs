namespace personal_website_blazor.Core.Application.Abstractions;

public interface IRssFeedService
{
    Task<string> BuildFeedAsync(Uri baseUri);
}
