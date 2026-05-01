using personal_website_blazor.Domain.Entities;

namespace personal_website_blazor.Application.Abstractions;

public interface IContentService
{
    Task<PostModel?> GetPostAsync(string section, string slug);
    Task<List<PostModel>> GetPostsAsync(string section);
    Task<List<ContentMetadata>> GetAllContentsAsync();
    Task<List<SearchResult>> SearchAsync(string query, string? section = null);
}
