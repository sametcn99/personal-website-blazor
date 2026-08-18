namespace personal_website_blazor.Interfaces;

public interface IMarkdownForAgentsService
{
    Task<string?> GetPageMarkdownAsync(string path);
    Task<int> EstimateTokenCountAsync(string markdown);
    Task<string> GetLlmsTxtAsync(string baseUrl);
}
