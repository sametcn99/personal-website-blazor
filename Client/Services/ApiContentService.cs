using System.Net.Http.Json;
using personal_website_blazor.Core.Application.Abstractions;
using personal_website_blazor.Core.Domain.Entities;

namespace personal_website_blazor.Client.Services;

internal sealed class ApiContentService(IHttpClientFactory httpClientFactory) : IContentService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("Server");

    public async Task<PostModel?> GetPostAsync(string section, string slug)
    {
        try
        {
            return await _client.GetFromJsonAsync<PostModel>($"api/content/{Uri.EscapeDataString(section)}/{Uri.EscapeDataString(slug)}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PostModel>> GetPostsAsync(string section)
    {
        try
        {
            return await _client.GetFromJsonAsync<List<PostModel>>($"api/content/{Uri.EscapeDataString(section)}")
                ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<ContentMetadata>> GetAllContentsAsync()
    {
        try
        {
            return await _client.GetFromJsonAsync<List<ContentMetadata>>("api/content/all") ?? [];
        }
        catch
        {
            return [];
        }
    }
}