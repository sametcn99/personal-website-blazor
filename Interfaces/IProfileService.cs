using personal_website_blazor.Models;

namespace personal_website_blazor.Interfaces;

public interface IProfileService
{
    Task<ProfileDocument> GetProfileAsync();
    Task<string> GetProfileMarkdownAsync(string baseUrl);
}
