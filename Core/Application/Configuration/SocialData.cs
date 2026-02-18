using personal_website_blazor.Core.Domain.Entities;

namespace personal_website_blazor.Core.Application.Configuration;

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
            Type = ["instagram", "ig"],
            Link = "https://instagram.com/sametc.0",
            Label = "Instagram",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#E4405F",
        },
        new()
        {
            Type = ["twitter", "x", "tw"],
            Link = "https://x.com/samet1178062",
            Label = "X/Twitter",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#111827",
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
            Type = ["discord", "dc"],
            Link = "https://discord.com/users/1120483504535392327",
            Label = "Discord",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#5865F2",
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
        new()
        {
            Type = ["youtube", "yt"],
            Link = "https://youtube.com/@sametc001",
            Label = "YouTube",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#FF0000",
        },
        new()
        {
            Type = ["youtubemusic", "ytmusic", "ytm"],
            Link = "https://music.youtube.com/channel/UCgXu7EZ76uMqPW8i4ZCL72Q?si=1aNE6Zya_1t9ACFl",
            Label = "YouTubeMusic",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#FF0000",
        },
        new()
        {
            Type = ["spotify", "sp"],
            Link = "https://open.spotify.com/user/31qg3kutxxwdq5lzydjx6md534cq",
            Label = "Spotify",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#1DB954",
        },
        new()
        {
            Type = ["letterboxd", "lbxd", "lb"],
            Link = "https://letterboxd.com/sametc001",
            Label = "Letterboxd",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#202830",
        },
        new()
        {
            Type = ["imdb"],
            Link = "https://www.imdb.com/user/ur120575296",
            Label = "IMDb",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#F5C518",
        },
        new()
        {
            Type = ["pinterest"],
            Link = "https://pinterest.com/sametcn99",
            Label = "Pinterest",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#E60023",
        },
        new()
        {
            Type = ["mastodon"],
            Link = "https://mastodon.social/@sametcn99",
            Label = "Mastodon",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#6364FF",
        },
        new()
        {
            Type = ["bluesky", "bsky"],
            Link = "https://bsky.app/profile/sametcn99.bsky.social",
            Label = "Bluesky",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#0285FF",
        },
        new()
        {
            Type = ["goodreads", "gr"],
            Link = "https://www.goodreads.com/user/show/75848289-samet",
            Label = "Goodreads",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#553B08",
        },
        new()
        {
            Type = ["backloggd"],
            Link = "https://backloggd.com/u/sametc001",
            Label = "Backloggd",
            Visible = false,
            External = true,
            Category = "Social Media",
        },
        new()
        {
            Type = ["steam"],
            Link = "https://steamcommunity.com/id/sametc001",
            Label = "Steam",
            Visible = false,
            External = true,
            Category = "Social Media",
            IconColor = "#171A21",
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
