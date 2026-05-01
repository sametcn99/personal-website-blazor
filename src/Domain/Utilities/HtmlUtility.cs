using System.Text.RegularExpressions;

namespace personal_website_blazor.Domain.Utilities;

public static class HtmlUtility
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

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
}