using personal_website_blazor.Models;

namespace personal_website_blazor.Interfaces;

public interface IGitHubService
{
    Task<List<GitHubRepository>> GetUserRepositoriesAsync(string username, int perPage = 100);
}
