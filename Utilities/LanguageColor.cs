namespace personal_website_blazor.Utilities;

/// <summary>
/// Maps programming language names to their canonical brand colors for dot indicators.
/// </summary>
public static class LanguageColor
{
    private static readonly Dictionary<string, string> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "C#", "#239120" },
        { "C++", "#005997" },
        { "CSS", "#563d7c" },
        { "Dart", "#00B4AB" },
        { "Elixir", "#6e4a7e" },
        { "Go", "#00ADD8" },
        { "HTML", "#e34c26" },
        { "Java", "#b07219" },
        { "JavaScript", "#f1e05a" },
        { "Kotlin", "#A97BFF" },
        { "Lua", "#000080" },
        { "Markdown", "#083fa1" },
        { "PHP", "#4F5D95" },
        { "Python", "#3572A5" },
        { "Ruby", "#701516" },
        { "Rust", "#dea584" },
        { "Shell", "#89e051" },
        { "Swift", "#F05138" },
        { "TypeScript", "#3178c6" },
        { "Vue", "#41b883" },
    };

    public static string GetColor(string? language) =>
        language is not null && Colors.TryGetValue(language, out var color)
            ? color
            : "#8b949e";
}
