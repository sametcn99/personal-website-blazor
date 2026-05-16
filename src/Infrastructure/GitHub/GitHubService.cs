using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit;
using personal_website_blazor.Application.Abstractions;
using personal_website_blazor.Application.Configuration;
using personal_website_blazor.Domain.Entities;

namespace personal_website_blazor.Infrastructure.GitHub;

public class GitHubService : IGitHubService
{
    private readonly GitHubClient _gitHubClient;
    private readonly IMemoryCache _cache;

    public GitHubService(IOptions<GitHubOptions> options, IMemoryCache cache)
    {
        _cache = cache;
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
        var cacheKey = $"repos:{username}:{perPage}";

        if (_cache.TryGetValue(cacheKey, out List<GitHubRepository>? cached))
        {
            return cached!;
        }

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

        var result = limitedRepositories
            .Select(repo => new GitHubRepository
            {
                Name = repo.Name,
                Description = repo.Description,
                Language = repo.Language,
                Fork = repo.Fork,
                StargazersCount = repo.StargazersCount,
                HtmlUrl = repo.HtmlUrl,
                CreatedAt = repo.CreatedAt,
                UpdatedAt = repo.UpdatedAt,
            })
            .ToList();

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }
}