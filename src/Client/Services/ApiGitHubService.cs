using System.Net.Http.Json;
using personal_website_blazor.Application.Abstractions;
using personal_website_blazor.Domain.Entities;

namespace personal_website_blazor.Client.Services;

internal sealed class ApiGitHubService(IHttpClientFactory httpClientFactory) : IGitHubService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("Server");

    public async Task<List<GitHubRepository>> GetUserRepositoriesAsync(string username, int perPage = 100)
    {
        try
        {
            var repos = await _client.GetFromJsonAsync<List<RepoDto>>("api/repos") ?? [];
            return repos
                .Take(Math.Clamp(perPage, 1, 100))
                .Select(repo => new GitHubRepository
                {
                    Name = repo.Name ?? string.Empty,
                    Description = repo.Description,
                    Language = repo.Language,
                    Fork = repo.Fork,
                    HtmlUrl = repo.HtmlUrl ?? string.Empty,
                    UpdatedAt = repo.UpdatedAt,
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private sealed class RepoDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Language { get; set; }
        public bool Fork { get; set; }
        public string? HtmlUrl { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}