using personal_website_blazor.Models;

namespace personal_website_blazor.Interfaces;

/// <summary>
/// Request record describing a project's metric sources, taken from MDX frontmatter.
/// </summary>
public sealed record ProjectMetricRequest(string Slug, string? Repo, string? NpmPackage);

public interface IProjectMetricsService
{
    /// <summary>
    /// Fetches live metrics (GitHub stars/releases/downloads, npm downloads) for a batch
    /// of projects. Failures degrade gracefully: individual projects resolve to null and
    /// the UI omits the metric row. Results are memory-cached at the source level.
    /// </summary>
    Task<Dictionary<string, ProjectMetrics?>> GetMetricsForProjectsAsync(
        IEnumerable<ProjectMetricRequest> requests,
        string githubUsername
    );
}
