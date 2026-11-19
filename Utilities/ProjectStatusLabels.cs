namespace personal_website_blazor.Utilities;

/// <summary>
/// Maps normalized projectStatus frontmatter values to localization resource keys.
/// Unknown or missing values yield null so callers can omit the label entirely.
/// </summary>
public static class ProjectStatusLabels
{
    public static string? GetKey(string? projectStatus) =>
        projectStatus?.ToLowerInvariant() switch
        {
            "active" => "ProjectStatus_Active",
            "completed" => "ProjectStatus_Completed",
            "experimental" => "ProjectStatus_Experimental",
            "production" => "ProjectStatus_Production",
            _ => null,
        };
}
