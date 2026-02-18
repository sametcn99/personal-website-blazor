using System.Text.RegularExpressions;

namespace personal_website_blazor.Core.Application.Configuration;

public static partial class UmamiEvent
{
    private const string DefaultFallback = "item";

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex("^-+|-+$", RegexOptions.Compiled)]
    private static partial Regex TrimDashRegex();

    public static string Segment(string? value, string fallback = DefaultFallback)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        normalized = NonAlphanumericRegex().Replace(normalized, "-");
        normalized = TrimDashRegex().Replace(normalized, string.Empty);

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    public static string Click(string prefix, string? value, string fallback = DefaultFallback)
    {
        return $"{prefix}-{Segment(value, fallback)}-click";
    }

    public static string ClickValue(string? value, string fallback = DefaultFallback)
    {
        return $"{Segment(value, fallback)}-click";
    }
}
