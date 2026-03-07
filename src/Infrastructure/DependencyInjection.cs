using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using personal_website_blazor.Application.Abstractions;
using personal_website_blazor.Application.Configuration;
using personal_website_blazor.Infrastructure.Content;
using personal_website_blazor.Infrastructure.Feeds;
using personal_website_blazor.Infrastructure.GitHub;
using personal_website_blazor.Infrastructure.Seo;

namespace personal_website_blazor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<GitHubOptions>()
            .Bind(configuration.GetSection(GitHubOptions.SectionName));

        services
            .AddOptions<CachePolicyOptions>()
            .Bind(configuration.GetSection(CachePolicyOptions.SectionName));

        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IRssFeedService, RssFeedService>();
        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<IGitHubService, GitHubService>();

        services.AddHttpClient(
            "GitHub",
            client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "personal-website-blazor");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            }
        );

        return services;
    }
}
