using personal_website_blazor.Models;

namespace personal_website_blazor.Interfaces;

public interface IContentService
{
    Task<PostModel?> GetPostAsync(string section, string slug);
    Task<List<PostModel>> GetPostsAsync(string section);
    Task<List<PostModel>> GetPostMetadataListAsync(string section);
    Task<List<ContentMetadata>> GetAllContentsAsync();
    Task<List<SearchResult>> SearchAsync(string query, string? section = null);
}
