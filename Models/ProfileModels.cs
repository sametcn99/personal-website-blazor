namespace personal_website_blazor.Models;

public sealed class ProfileDocument
{
    public string Name { get; set; } = string.Empty;
    public string AlternateName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public string[] Skills { get; set; } = Array.Empty<string>();
    public string[] AreasOfInterest { get; set; } = Array.Empty<string>();
    public string[] Languages { get; set; } = Array.Empty<string>();
    public string[] PublicNotes { get; set; } = Array.Empty<string>();
    public ProfileLink[] Links { get; set; } = Array.Empty<ProfileLink>();
    public ExperienceEntry[] Experience { get; set; } = Array.Empty<ExperienceEntry>();
    public EducationEntry[] Education { get; set; } = Array.Empty<EducationEntry>();
    public CertificationEntry[] Certifications { get; set; } = Array.Empty<CertificationEntry>();
    public string LastUpdated { get; set; } = string.Empty;
}

public sealed class ProfileLink
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public sealed class ExperienceEntry
{
    public string Organization { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string[] Highlights { get; set; } = Array.Empty<string>();
}

public sealed class EducationEntry
{
    public string Institution { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string[] Highlights { get; set; } = Array.Empty<string>();
}

public sealed class CertificationEntry
{
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string? Url { get; set; }
}

public sealed class TimelineEntry
{
    public string Type { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string[] Highlights { get; set; } = Array.Empty<string>();
}
