using personal_website_blazor.Domain.Entities;

namespace personal_website_blazor.Application.Abstractions;

public interface IGitHubService
{
    Task<List<GitHubRepository>> GetUserRepositoriesAsync(string username, int perPage = 100);
}
