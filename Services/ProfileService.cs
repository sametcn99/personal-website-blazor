using System.Text.Json;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IWebHostEnvironment _env;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ProfileService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<ProfileDocument> GetProfileAsync()
    {
        var path = Path.Combine(_env.ContentRootPath, "content", "profile.json");
        if (!File.Exists(path))
            return new ProfileDocument();

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ProfileDocument>(stream, _jsonOptions)
            ?? new ProfileDocument();
    }

    public async Task<string> GetProfileMarkdownAsync(string baseUrl)
    {
        var profile = await GetProfileAsync();
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"title: About {profile.Name}");
        sb.AppendLine($"description: {profile.Summary}");
        sb.AppendLine("type: profile");
        sb.AppendLine($"lastUpdated: {profile.LastUpdated}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {profile.Name}");
        sb.AppendLine();
        sb.AppendLine($"**Role:** {profile.JobTitle}");
        sb.AppendLine($"**Location:** {profile.Location}");
        sb.AppendLine();
        sb.AppendLine(profile.Summary);
        sb.AppendLine();
        sb.AppendLine(profile.Biography);
        sb.AppendLine();

        AppendBulletSection(sb, "Skills", profile.Skills);
        AppendBulletSection(sb, "Areas Of Interest", profile.AreasOfInterest);
        AppendBulletSection(sb, "Languages", profile.Languages);
        AppendBulletSection(sb, "Additional Public Notes", profile.PublicNotes);

        sb.AppendLine("## Experience");
        sb.AppendLine();
        foreach (var item in profile.Experience)
        {
            sb.AppendLine($"### {item.Role} — {item.Organization}");
            sb.AppendLine();
            sb.AppendLine($"**Period:** {item.StartDate} to {item.EndDate}");
            if (!string.IsNullOrWhiteSpace(item.Location))
                sb.AppendLine($"**Location:** {item.Location}");
            sb.AppendLine();
            foreach (var highlight in item.Highlights)
                sb.AppendLine($"- {highlight}");
            sb.AppendLine();
        }

        sb.AppendLine("## Education");
        sb.AppendLine();
        foreach (var item in profile.Education)
        {
            sb.AppendLine($"### {item.Program} — {item.Institution}");
            sb.AppendLine();
            sb.AppendLine($"**Level:** {item.Level}");
            sb.AppendLine($"**Period:** {item.StartDate} to {item.EndDate}");
            foreach (var highlight in item.Highlights)
                sb.AppendLine($"- {highlight}");
            sb.AppendLine();
        }

        sb.AppendLine("## Certifications");
        sb.AppendLine();
        foreach (var item in profile.Certifications)
        {
            var suffix = string.IsNullOrWhiteSpace(item.Url) ? string.Empty : $" ([certificate]({item.Url}))";
            sb.AppendLine($"- **{item.Name}** — {item.Issuer}{suffix}");
        }
        sb.AppendLine();

        sb.AppendLine("## Profiles And Sources");
        sb.AppendLine();
        sb.AppendLine($"- [Full CV]({normalizedBaseUrl}/cv.md)");
        sb.AppendLine($"- [GitHub profile]({normalizedBaseUrl}/readme.md)");
        foreach (var link in profile.Links)
            sb.AppendLine($"- [{link.Label}]({link.Url})");

        return sb.ToString();
    }

    private static void AppendBulletSection(System.Text.StringBuilder sb, string heading, IEnumerable<string> values)
    {
        sb.AppendLine($"## {heading}");
        sb.AppendLine();
        foreach (var value in values)
            sb.AppendLine($"- {value}");
        sb.AppendLine();
    }
}
