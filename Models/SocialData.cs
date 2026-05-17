namespace personal_website_blazor.Models;

public static class SocialData
{
    public static readonly SocialMediaLink[] Links =
    [
        new()
        {
            Type = ["linkedin", "li"],
            Link = "https://www.linkedin.com/in/sametc0",
            Label = "LinkedIn",
            Visible = true,
            External = true,
            Category = "Professional Networks",
            IconColor = "#0A66C2",
        },
        new()
        {
            Type = ["github", "gh"],
            Link = "https://github.com/sametcn99",
            Label = "GitHub",
            Visible = true,
            External = true,
            Category = "Development Platforms",
            IconColor = "#181717",
        },
        new()
        {
            Type = ["repo", "repos", "repositories"],
            Link = "/repo",
            Label = "Repositories",
            Visible = true,
            External = false,
            Category = "Development Platforms",
            IconColor = "#181717",
        },
        new()
        {
            Type =
            [
                "vscode-extensions",
                "vscodeextensions",
                "vsextensions",
                "vsext",
                "vscode",
                "vsce",
            ],
            Link = "https://marketplace.visualstudio.com/publishers/sametcn99",
            Label = "VSCode Extensions",
            Visible = false,
            External = true,
            Category = "Development Platforms",
            IconColor = "#007ACC",
        },
        new()
        {
            Type = ["npm", "npmjs"],
            Link = "https://www.npmjs.com/~sametc0",
            Label = "NPMJS",
            Visible = false,
            External = true,
            Category = "Development Platforms",
            IconColor = "#CB3837",
        },
        new()
        {
            Type = ["statsfm", "sfm"],
            Link = "https://stats.fm/sametc001",
            Label = "Stats.fm",
            Visible = false,
            External = true,
            Category = "Social Media",
        },
        new()
        {
            Type = ["leetcode", "lc"],
            Link = "https://leetcode.com/sametcn99",
            Label = "LeetCode",
            Visible = false,
            External = true,
            Category = "Development Platforms",
            IconColor = "#FFA116",
        },
    ];

    public static readonly Dictionary<string, int> CategoryOrder = new()
    {
        ["Professional Networks"] = 1,
        ["Development Platforms"] = 2,
        ["Contact"] = 3,
        ["Social Media"] = 4,
        ["Other"] = 5,
    };
}
