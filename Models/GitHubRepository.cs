namespace personal_website_blazor.Models;

public class GitHubRepository
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Language { get; set; }
    public bool Fork { get; set; }
    public bool Archived { get; set; }
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public int OpenIssuesCount { get; set; }
    public int WatchersCount { get; set; }
    public long Size { get; set; }
    public string? Homepage { get; set; }
    public string? DefaultBranch { get; set; }
    public string? License { get; set; }
    public string? CloneUrl { get; set; }
    public string? SshUrl { get; set; }
    public string[] Topics { get; set; } = Array.Empty<string>();
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? PushedAt { get; set; }
}

public sealed class GitHubProfile
{
    public string Login { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Blog { get; set; }
    public string? AvatarUrl { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public int PublicRepos { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class GitHubProfileDocument
{
    public GitHubProfile? Profile { get; set; }
    public List<GitHubRepository> Repositories { get; set; } = new();
    public string Source { get; set; } = "https://github.com/sametcn99";
    public string RetrievedAt { get; set; } = string.Empty;
}
