namespace personal_website_blazor.Core.Domain.Entities;

public class PostModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? PublishDate { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Content { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string? Image { get; set; }
    public string? Author { get; set; }
}

public class ContentMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string PublishedAt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Language { get; set; } = "en";
}

public class SocialMediaLink
{
    public string[] Type { get; set; } = Array.Empty<string>();
    public string Link { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Visible { get; set; }
    public bool External { get; set; }
    public string Category { get; set; } = "Other";
    public string? IconColor { get; set; }
}
