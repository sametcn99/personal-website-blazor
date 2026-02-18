using personal_website_blazor.Core.Domain.Entities;

namespace personal_website_blazor.Core.Application.Abstractions;

public interface IContentService
{
    Task<PostModel?> GetPostAsync(string section, string slug);
    Task<List<PostModel>> GetPostsAsync(string section);
    Task<List<ContentMetadata>> GetAllContentsAsync();
}
