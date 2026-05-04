using System.Text.RegularExpressions;
using personal_website_blazor.Domain.Entities;

namespace personal_website_blazor.Domain.Utilities;

public static class HtmlUtility
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly string[] HeadingTags = ["h2", "h3", "h4", "h5", "h6"];

    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = HtmlTagRegex.Replace(html, " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = WhitespaceRegex.Replace(text, " ").Trim();

        return text;
    }

    public static string GetSnippet(string? text, string query, int contextLength = 80)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(query))
            return text.Length <= contextLength * 2 ? text : text[..(contextLength * 2)] + "...";

        var span = text.AsSpan();
        var querySpan = query.AsSpan();
        var idx = span.IndexOf(querySpan, StringComparison.OrdinalIgnoreCase);

        if (idx < 0)
            return text.Length <= contextLength * 2 ? text : text[..(contextLength * 2)] + "...";

        var start = Math.Max(0, idx - contextLength);
        var end = Math.Min(text.Length, idx + query.Length + contextLength);
        var snippet = text[start..end];

        var prefix = start > 0 ? "..." : "";
        var suffix = end < text.Length ? "..." : "";

        return $"{prefix}{snippet}{suffix}";
    }

    public static List<TocItem> ExtractHeadings(string? html)
    {
        var items = new List<TocItem>();
        if (string.IsNullOrWhiteSpace(html))
            return items;

        var pattern = @"<(h[2-6])\s[^>]*?id=""([^""]+)""[^>]*>(.*?)</\1>";
        var matches = Regex.Matches(html, pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var tag = match.Groups[1].Value.ToLowerInvariant();
            var level = int.Parse(tag[1..]);
            var id = match.Groups[2].Value;
            var text = StripHtml(match.Groups[3].Value);

            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(text))
            {
                items.Add(new TocItem { Id = id, Text = text, Level = level });
            }
        }

        return items;
    }
}