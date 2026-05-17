using personal_website_blazor.Models;

namespace personal_website_blazor.Interfaces;

public interface ISocialLinkProvider
{
    IReadOnlyList<SocialMediaLink> Links { get; }
    int GetCategoryOrder(string category);
}
