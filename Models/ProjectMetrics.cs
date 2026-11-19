namespace personal_website_blazor.Models;

/// <summary>
/// Live, API-sourced metrics for a single project. All values are optional —
/// missing data simply means the UI omits that metric.
/// </summary>
public sealed class ProjectMetrics
{
    public int Stars { get; set; }
    public int Forks { get; set; }
    public int ReleaseCount { get; set; }
    public string? LatestReleaseTag { get; set; }
    public DateTime? LatestReleaseAt { get; set; }
    public long TotalReleaseDownloads { get; set; }
    public long? NpmMonthlyDownloads { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Release-related facts fetched from the GitHub API for one repository.
/// Cached independently so stars can be joined from the (separately cached)
/// repository snapshot without extra API calls.
/// </summary>
public sealed class RepositoryInsights
{
    public int ReleaseCount { get; set; }
    public string? LatestReleaseTag { get; set; }
    public DateTime? LatestReleaseAt { get; set; }
    public long TotalReleaseDownloads { get; set; }
}
