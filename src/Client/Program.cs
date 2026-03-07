using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using personal_website_blazor.Client.Services;
using personal_website_blazor.Application.Abstractions;

namespace personal_website_blazor.Client;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddMudServices();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("Server", client =>
        {
            client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
        });
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Server"));
        builder.Services.AddScoped<IContentService, ApiContentService>();
        builder.Services.AddScoped<IGitHubService, ApiGitHubService>();
        await builder.Build().RunAsync();
    }
}