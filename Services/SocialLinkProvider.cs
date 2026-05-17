using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Services;

public sealed class SocialLinkProvider : ISocialLinkProvider
{
    public IReadOnlyList<SocialMediaLink> Links { get; } =
    [
        new()
        {
            Type = ["linkedin"],
            Link = "https://www.linkedin.com/in/sametc0",
            Label = "LinkedIn",
            External = true,
            Category = "Professional Networks",
            IconColor = "#0A66C2",
        },
        new()
        {
            Type = ["github", "gh"],
            Link = "https://github.com/sametcn99",
            Label = "GitHub",
            External = true,
            Category = "Development Platforms",
            IconColor = "#181717",
        },
        new()
        {
            Type = ["repositories", "repo"],
            Link = "/repo",
            Label = "Repositories",
            Category = "Development Platforms",
            IconColor = "#181717",
        },
        new()
        {
            Type = ["vscode-extensions", "vscode", "vsce"],
            Link = "https://marketplace.visualstudio.com/publishers/sametcn99",
            Label = "VSCode Extensions",
            External = true,
            Category = "Development Platforms",
            IconColor = "#007ACC",
        },
        new()
        {
            Type = ["npm"],
            Link = "https://www.npmjs.com/~sametc0",
            Label = "NPMJS",
            External = true,
            Category = "Development Platforms",
            IconColor = "#CB3837",
        },
        new()
        {
            Type = ["statsfm", "sfm"],
            Link = "https://stats.fm/sametc001",
            Label = "Stats.fm",
            External = true,
            Category = "Social Media",
        },
        new()
        {
            Type = ["leetcode", "lc"],
            Link = "https://leetcode.com/sametcn99",
            Label = "LeetCode",
            External = true,
            Category = "Development Platforms",
            IconColor = "#FFA116",
        },
    ];

    private static readonly Dictionary<string, int> CategoryOrder = new()
    {
        ["Professional Networks"] = 1,
        ["Development Platforms"] = 2,
        ["Contact"] = 3,
        ["Social Media"] = 4,
        ["Other"] = 5,
    };

    public int GetCategoryOrder(string category)
        => CategoryOrder.GetValueOrDefault(category, 5);
}
