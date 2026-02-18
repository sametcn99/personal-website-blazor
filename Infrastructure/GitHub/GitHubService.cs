using Microsoft.Extensions.Options;
using Octokit;
using personal_website_blazor.Core.Application.Abstractions;
using personal_website_blazor.Core.Application.Configuration;
using personal_website_blazor.Core.Domain.Entities;

namespace personal_website_blazor.Infrastructure.GitHub;

public class GitHubService : IGitHubService
{
    private readonly GitHubClient _gitHubClient;

    public GitHubService(IOptions<GitHubOptions> options)
    {
        _gitHubClient = new GitHubClient(new ProductHeaderValue("personal-website-blazor"));

        var token = options.Value.Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            _gitHubClient.Credentials = new Credentials(token);
        }
    }

    public async Task<List<GitHubRepository>> GetUserRepositoriesAsync(
        string username,
        int perPage = 100
    )
    {
        var options = new ApiOptions
        {
            PageCount = 1,
            PageSize = 100,
            StartPage = 1,
        };

        var repositories = await _gitHubClient.Repository.GetAllForUser(username, options);
        var limitedRepositories = repositories
            .OrderByDescending(repo => repo.UpdatedAt)
            .Take(Math.Clamp(perPage, 1, 100));

        return limitedRepositories
            .Select(repo => new GitHubRepository
            {
                Name = repo.Name,
                Description = repo.Description,
                Language = repo.Language,
                Fork = repo.Fork,
                HtmlUrl = repo.HtmlUrl,
                UpdatedAt = repo.UpdatedAt,
            })
            .ToList();
    }
}
