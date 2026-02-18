using personal_website_blazor.Core.Domain.Entities;

namespace personal_website_blazor.Core.Application.Abstractions;

public interface IGitHubService
{
    Task<List<GitHubRepository>> GetUserRepositoriesAsync(string username, int perPage = 100);
}
