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
            Type = ["leetcode", "lc"],
            Link = "https://leetcode.com/sametcn99",
            Label = "LeetCode",
            External = true,
            Category = "Development Platforms",
            IconColor = "#FFA116",
        },
                new()
        {
            Type = ["telegram", "tg"],
            Link = "https://t.me/sametc0",
            Label = "Telegram",
            External = true,
            Category = "Contact",
            IconColor = "#26A5E4",
        },
        new()
        {
            Type = ["mail", "email", "gmail", "e-mail"],
            Link = "mailto:sametcn99@gmail.com",
            Label = "Mail",
            External = false,
            Category = "Contact",
            IconColor = "#EA4335",
        },
        new()
        {
            Type = ["cv", "ozgecmis", "letter", "resume"],
            Link = "/cv",
            Label = "Resume",
            External = false,
            Category = "Professional Networks",
            IconColor = "#1A73E8",
        },
        new()
        {
            Type = ["readme", "about"],
            Link = "/readme",
            Label = "Readme",
            External = false,
            Category = "Development Platforms",
            IconColor = "#24292F",
        },
        new()
        {
            Type = ["support", "sponsor", "donate"],
            Link = "/support",
            Label = "Support Me",
            External = false,
            Category = "Contact",
            IconColor = "#FFDD00",
        },
        new(){
          Type = ["Whatsapp", "wa", "wp"],
          Link= "https://wa.me/905303790565",
          Label = "Whatsapp",
          External = true,
          Category = "Contact",
          IconColor = "#25D366",
        },
       new()
       {
        Type = ["YouTube", "yt"],
            Link = "https://www.youtube.com/@sametc001",
            Label = "YouTube",
            External = true,
            Category = "Social Media",
            IconColor = "#FF0000",
        },
        new(){
            Type = ["YouTubeMusic", "ytm"],
            Link = "https://music.youtube.com/@sametc001",
            Label = "YouTube Music",
            External = true,
            Category = "Social Media",
            IconColor = "#FF0000",
        }
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
