namespace personal_website_blazor.Models;

public class GitHubRepository
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Language { get; set; }
    public bool Fork { get; set; }
    public int StargazersCount { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
