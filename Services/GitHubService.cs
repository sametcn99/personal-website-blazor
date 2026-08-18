using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

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
                Archived = repo.Archived,
                StargazersCount = repo.StargazersCount,
                ForksCount = repo.ForksCount,
                OpenIssuesCount = repo.OpenIssuesCount,
                WatchersCount = repo.SubscribersCount,
                Size = repo.Size,
                Homepage = repo.Homepage,
                DefaultBranch = repo.DefaultBranch,
                License = repo.License?.Name,
                CloneUrl = repo.CloneUrl,
                SshUrl = repo.SshUrl,
                Topics = repo.Topics?.ToArray() ?? Array.Empty<string>(),
                HtmlUrl = repo.HtmlUrl,
                CreatedAt = repo.CreatedAt,
                UpdatedAt = repo.UpdatedAt,
                PushedAt = repo.PushedAt,
            })
            .ToList();

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public async Task<GitHubProfile?> GetUserProfileAsync(string username)
    {
        var cacheKey = $"profile:{username}";
        if (_cache.TryGetValue(cacheKey, out GitHubProfile? cached))
            return cached;

        var user = await _gitHubClient.User.Get(username);
        if (user is null)
            return null;

        var profile = new GitHubProfile
        {
            Login = user.Login,
            Name = user.Name,
            Bio = user.Bio,
            Company = user.Company,
            Location = user.Location,
            Blog = user.Blog,
            AvatarUrl = user.AvatarUrl,
            HtmlUrl = user.HtmlUrl,
            PublicRepos = user.PublicRepos,
            Followers = user.Followers,
            Following = user.Following,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };

        _cache.Set(cacheKey, profile, TimeSpan.FromMinutes(15));
        return profile;
    }
}
