using personal_website_blazor.Domain.Entities;

namespace personal_website_blazor.Application.Configuration;

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
            Type = ["repo", "repos", "repositories", "repository"],
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
            Type = ["leetcode", "lc"],
            Link = "https://leetcode.com/sametcn99",
            Label = "LeetCode",
            Visible = false,
            External = true,
            Category = "Development Platforms",
            IconColor = "#FFA116",
        },
        new()
        {
            Type = ["telegram", "tg"],
            Link = "https://t.me/sametc0",
            Label = "Telegram",
            Visible = true,
            External = true,
            Category = "Contact",
            IconColor = "#26A5E4",
        },
        new()
        {
            Type = ["mail", "email", "gmail", "e-mail"],
            Link = "mailto:sametcn99@gmail.com",
            Label = "Mail",
            Visible = true,
            External = false,
            Category = "Contact",
            IconColor = "#EA4335",
        },
        new()
        {
            Type = ["cv", "ozgecmis", "letter", "resume"],
            Link = "/cv",
            Label = "Resume",
            Visible = true,
            External = false,
            Category = "Professional Networks",
            IconColor = "#1A73E8",
        },
        new()
        {
            Type = ["readme", "about"],
            Link = "/readme",
            Label = "Readme",
            Visible = true,
            External = false,
            Category = "Development Platforms",
            IconColor = "#24292F",
        },
        new()
        {
            Type = ["support", "sponsor", "donate"],
            Link = "/support",
            Label = "Support Me",
            Visible = true,
            External = false,
            Category = "Contact",
            IconColor = "#FFDD00",
        },
        new()
        {
            Type = ["whatsapp", "wp"],
            Link = "https://wa.me/+905303790565",
            Label = "WhatsApp",
            Visible = false,
            External = true,
            Category = "Contact",
            IconColor = "#25D366",
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
