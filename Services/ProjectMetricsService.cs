using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

public class ProjectMetricsService : IProjectMetricsService
{
    private readonly IGitHubService _gitHubService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public ProjectMetricsService(
        IGitHubService gitHubService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache
    )
    {
        _gitHubService = gitHubService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<Dictionary<string, ProjectMetrics?>> GetMetricsForProjectsAsync(
        IEnumerable<ProjectMetricRequest> requests,
        string githubUsername
    )
    {
        var projectRequests = requests.ToList();
        if (projectRequests.Count == 0)
            return new Dictionary<string, ProjectMetrics?>();

        // One cached snapshot call covers star counts for every same-owner repo.
        List<GitHubRepository>? repoSnapshot = null;
        try
        {
            repoSnapshot = await _gitHubService.GetUserRepositoriesAsync(githubUsername, 100);
        }
        catch
        {
            // The star join is optional — release and npm metrics still work without it.
        }

        var pairs = await Task.WhenAll(
            projectRequests.Select(async request =>
                (
                    request.Slug,
                    Metrics: await GetMetricsAsync(request, githubUsername, repoSnapshot)
                )
            )
        );

        return pairs.ToDictionary(pair => pair.Slug, pair => pair.Metrics);
    }

    private async Task<ProjectMetrics?> GetMetricsAsync(
        ProjectMetricRequest request,
        string githubUsername,
        List<GitHubRepository>? repoSnapshot
    )
    {
        try
        {
            var metrics = new ProjectMetrics { RetrievedAt = DateTime.UtcNow };

            if (!string.IsNullOrWhiteSpace(request.Repo))
            {
                var (owner, name) = SplitRepo(request.Repo);
                if (owner is not null && name is not null)
                {
                    var insights = await _gitHubService.GetRepositoryInsightsAsync(owner, name);
                    if (insights is not null)
                    {
                        metrics.ReleaseCount = insights.ReleaseCount;
                        metrics.LatestReleaseTag = insights.LatestReleaseTag;
                        metrics.LatestReleaseAt = insights.LatestReleaseAt;
                        metrics.TotalReleaseDownloads = insights.TotalReleaseDownloads;
                    }

                    if (
                        repoSnapshot is not null
                        && string.Equals(owner, githubUsername, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        var match = repoSnapshot.FirstOrDefault(repo =>
                            string.Equals(repo.Name, name, StringComparison.OrdinalIgnoreCase)
                        );
                        if (match is not null)
                        {
                            metrics.Stars = match.StargazersCount;
                            metrics.Forks = match.ForksCount;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(request.NpmPackage))
                metrics.NpmMonthlyDownloads = await GetNpmMonthlyDownloadsAsync(request.NpmPackage);

            if (
                metrics.ReleaseCount == 0
                && metrics.Stars == 0
                && metrics.NpmMonthlyDownloads is null
            )
                return null;

            return metrics;
        }
        catch
        {
            return null;
        }
    }

    private async Task<long?> GetNpmMonthlyDownloadsAsync(string package)
    {
        var cacheKey = $"npm-downloads:{package}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return cached as long?;

        try
        {
            var client = _httpClientFactory.CreateClient("project-metrics");
            using var response = await client.GetAsync(
                $"https://api.npmjs.org/downloads/point/last-month/{Uri.EscapeDataString(package)}"
            );
            if (!response.IsSuccessStatusCode)
            {
                _cache.Set(cacheKey, (long?)null, TimeSpan.FromMinutes(5));
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<NpmDownloadsResponse>();
            long? downloads = payload?.Downloads;
            _cache.Set(cacheKey, downloads, TimeSpan.FromMinutes(30));
            return downloads;
        }
        catch
        {
            _cache.Set(cacheKey, (long?)null, TimeSpan.FromMinutes(5));
            return null;
        }
    }

    private static (string? Owner, string? Name) SplitRepo(string repo)
    {
        var parts = repo.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return parts.Length == 2 ? (parts[0], parts[1]) : (null, null);
    }

    private sealed class NpmDownloadsResponse
    {
        public long Downloads { get; set; }
    }
}
