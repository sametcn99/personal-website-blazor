using System.Security;
using System.Text;

namespace personal_website_blazor.Services;

public static class OpenGraphImageBuilder
{
    private const string Background = SiteThemeSvg.Background;
    private const string Surface = SiteThemeSvg.Surface;
    private const string TextPrimary = SiteThemeSvg.TextPrimary;
    private const string TextSecondary = SiteThemeSvg.TextSecondary;
    private const string Brass = SiteThemeSvg.Brass;
    private const string Sage = SiteThemeSvg.Sage;
    private const string Divider = SiteThemeSvg.Divider;
    private const string FontSans = SiteThemeSvg.FontSans;
    private const string FontMeasure = SiteThemeSvg.FontMeasure;

    public static string Build(
        string? title,
        string? description,
        string? type,
        string? date,
        string? path)
    {
        var safeTitle = Clean(title, "Samet Can Cıncık — Software Developer Notes", 140);
        var safeDescription = Clean(
            description,
            "Software engineering essays, practical references, and project notes.",
            240);
        var safeDate = Clean(date, string.Empty, 32);
        var safePath = Clean(path, "sametcc.me", 96);
        var kind = GetKind(type);
        var titleLines = Wrap(safeTitle, 34, 2);
        var descriptionLines = Wrap(safeDescription, 68, 3);
        var titleStartY = titleLines.Count == 1 ? 306 : 270;
        var descriptionStartY = titleStartY + (titleLines.Count * 68) + 26;

        var svg = new StringBuilder(6000);
        svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        svg.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1200\" height=\"630\" viewBox=\"0 0 1200 630\" role=\"img\" aria-labelledby=\"og-title og-description\">");
        svg.AppendLine("  <title id=\"og-title\">" + Escape(safeTitle) + "</title>");
        svg.AppendLine("  <desc id=\"og-description\">" + Escape(safeDescription) + "</desc>");
        svg.AppendLine("  <defs>");
        svg.AppendLine("    <pattern id=\"field-grid\" width=\"32\" height=\"32\" patternUnits=\"userSpaceOnUse\">");
        svg.AppendLine($"      <path d=\"M 32 0 L 0 0 0 32\" fill=\"none\" stroke=\"{Divider}\" stroke-width=\"1\" opacity=\"0.5\" />");
        svg.AppendLine("    </pattern>");
        svg.AppendLine("  </defs>");
        svg.AppendLine($"  <rect width=\"1200\" height=\"630\" fill=\"{Background}\" />");
        svg.AppendLine("  <rect x=\"42\" y=\"42\" width=\"1116\" height=\"546\" fill=\"url(#field-grid)\" opacity=\"0.42\" />");
        svg.AppendLine($"  <rect x=\"72\" y=\"72\" width=\"1056\" height=\"486\" rx=\"12\" fill=\"{Surface}\" stroke=\"{Divider}\" stroke-width=\"2\" />");
        svg.AppendLine($"  <rect x=\"72\" y=\"72\" width=\"1056\" height=\"6\" rx=\"3\" fill=\"{Brass}\" />");
        svg.AppendLine($"  <text x=\"108\" y=\"128\" fill=\"{TextPrimary}\" font-family=\"{FontSans}\" font-size=\"21\" font-weight=\"700\" letter-spacing=\"1.5\">SAMET CAN CINCİK</text>");
        svg.AppendLine($"  <text x=\"1092\" y=\"128\" text-anchor=\"end\" fill=\"{TextSecondary}\" font-family=\"{FontMeasure}\" font-size=\"16\" letter-spacing=\"1.4\">FIELD NOTES</text>");
        svg.AppendLine($"  <line x1=\"108\" y1=\"158\" x2=\"1092\" y2=\"158\" stroke=\"{Divider}\" stroke-width=\"2\" />");
        svg.AppendLine($"  <text x=\"108\" y=\"204\" fill=\"{Brass}\" font-family=\"{FontMeasure}\" font-size=\"17\" font-weight=\"700\" letter-spacing=\"2.4\">{Escape(kind)}</text>");

        for (var index = 0; index < titleLines.Count; index++)
        {
            svg.AppendLine($"  <text x=\"108\" y=\"{titleStartY + index * 68}\" fill=\"{TextPrimary}\" font-family=\"{FontSans}\" font-size=\"58\" font-weight=\"700\" letter-spacing=\"-1.6\">{Escape(titleLines[index])}</text>");
        }

        for (var index = 0; index < descriptionLines.Count; index++)
        {
            svg.AppendLine($"  <text x=\"110\" y=\"{descriptionStartY + index * 28}\" fill=\"{TextSecondary}\" font-family=\"{FontSans}\" font-size=\"22\">{Escape(descriptionLines[index])}</text>");
        }

        svg.AppendLine($"  <line x1=\"108\" y1=\"492\" x2=\"1092\" y2=\"492\" stroke=\"{Divider}\" stroke-width=\"2\" />");
        svg.AppendLine($"  <circle cx=\"116\" cy=\"526\" r=\"6\" fill=\"{Sage}\" />");
        svg.AppendLine($"  <text x=\"136\" y=\"532\" fill=\"{TextSecondary}\" font-family=\"{FontMeasure}\" font-size=\"16\">{Escape(safePath)}</text>");
        if (!string.IsNullOrWhiteSpace(safeDate))
        {
            svg.AppendLine($"  <text x=\"1092\" y=\"532\" text-anchor=\"end\" fill=\"{TextSecondary}\" font-family=\"{FontMeasure}\" font-size=\"16\">{Escape(safeDate)}</text>");
        }

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    private static string GetKind(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "blog" or "post" => "BLOG POST",
        "gist" or "note" => "FIELD NOTE",
        "project" => "PROJECT RECORD",
        "profile" => "PROFILE",
        _ => "SOFTWARE NOTES",
    };

    private static IReadOnlyList<string> Wrap(string value, int maxCharacters, int maxLines)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>(maxLines);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            var candidateLength = current.Length == 0 ? word.Length : current.Length + 1 + word.Length;
            if (candidateLength <= maxCharacters || current.Length == 0)
            {
                if (current.Length > 0)
                    current.Append(' ');
                current.Append(word);
                continue;
            }

            lines.Add(current.ToString());
            current.Clear();
            current.Append(word);

            if (lines.Count == maxLines - 1)
                break;
        }

        if (lines.Count < maxLines && current.Length > 0)
            lines.Add(current.ToString());

        if (lines.Count == 0)
            lines.Add(string.Empty);

        if (lines.Count == maxLines && words.Length > 0)
        {
            var rendered = string.Join(' ', lines);
            if (rendered.Length < value.Length)
                lines[^1] = TrimForEllipsis(lines[^1], maxCharacters);
        }

        return lines;
    }

    private static string TrimForEllipsis(string value, int maxCharacters)
    {
        const string ellipsis = "…";
        var maxLength = Math.Max(1, maxCharacters - ellipsis.Length);
        return value.Length > maxLength
            ? value[..maxLength].TrimEnd() + ellipsis
            : value.TrimEnd() + ellipsis;
    }

    private static string Clean(string? value, string fallback, int maxLength)
    {
        var cleaned = (value ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = fallback;

        return cleaned.Length <= maxLength
            ? cleaned
            : cleaned[..Math.Max(1, maxLength - 1)].TrimEnd() + "…";
    }

    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;
}
