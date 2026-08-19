using personal_website_blazor.Models;

namespace personal_website_blazor.Interfaces;

public interface IMcpQueryService
{
    Task<ProfileDocument> GetProfileAsync();
    Task<McpPage<McpContentSummary>> ListProjectsAsync(McpProjectFilter filter);
    Task<McpContentDocument?> GetContentAsync(string section, string slug, bool includeBody, bool includeRelated);
    Task<McpPage<McpContentSummary>> SearchContentAsync(McpSearchFilter filter);
    Task<IReadOnlyList<TimelineEntry>> GetTimelineAsync();
    Task<McpSkillsResult> GetSkillsAsync();
    Task<McpTaxonomyResult> GetTaxonomyAsync(string[] sections);
    Task<McpRelatedContentResult?> GetRelatedContentAsync(string section, string slug, int limit);
}
