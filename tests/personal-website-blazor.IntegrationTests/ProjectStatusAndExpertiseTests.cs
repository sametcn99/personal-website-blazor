using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using personal_website_blazor.Interfaces;
using Xunit;

namespace personal_website_blazor.IntegrationTests;

public sealed class ProjectStatusAndExpertiseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;

    public ProjectStatusAndExpertiseTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _services = factory.Services;
    }

    [Fact]
    public async Task ProjectList_ShowsStatusBadgesAndFilter()
    {
        var response = await _client.GetAsync("/project");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Actively developed", html);
        Assert.Contains("Completed", html);
        Assert.Contains("Production-ready", html);
        Assert.Contains("All statuses", html);
    }

    [Fact]
    public async Task ProjectDetail_ShowsProjectRecordBlock()
    {
        var response = await _client.GetAsync("/project/repotruck");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Actively developed", html);
        Assert.Contains("https://github.com/sametcn99/repotruck", html);
        Assert.Contains("https://www.npmjs.com/package/repotruck", html);
    }

    [Fact]
    public async Task HomePage_RendersExpertiseSection()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Full-stack product engineering", html);
        Assert.Contains("Backend architecture and infrastructure", html);
        Assert.Contains("AI-native developer tooling", html);
        Assert.Contains("/project/htwind", html);
    }

    [Fact]
    public async Task SkillsPage_RendersExpertisePillarsWithEvidence()
    {
        var response = await _client.GetAsync("/skills");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Full-stack product engineering", html);
        Assert.Contains("AI-native developer tooling", html);
        Assert.Contains("Proven by", html);
        Assert.Contains("/project/tcptui", html);
    }

    [Fact]
    public async Task AboutPage_RendersPillarSummary()
    {
        var response = await _client.GetAsync("/about");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Backend architecture and infrastructure", html);
        Assert.Contains("/skills", html);
    }

    [Fact]
    public async Task ContentService_ParsesProjectStatusRepoAndNpm()
    {
        using var scope = _services.CreateScope();
        var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();

        var repotruck = await contentService.GetPostAsync("projects", "repotruck");
        Assert.NotNull(repotruck);
        Assert.Equal("active", repotruck.ProjectStatus);
        Assert.Equal("sametcn99/repotruck", repotruck.Repo);
        Assert.Equal("repotruck", repotruck.NpmPackage);

        var hiretea = await contentService.GetPostAsync("projects", "hiretea");
        Assert.NotNull(hiretea);
        Assert.Equal("production", hiretea.ProjectStatus);
        Assert.Null(hiretea.Repo);
        Assert.Null(hiretea.NpmPackage);
    }

    [Fact]
    public async Task ProjectMetricsService_DegradesGracefullyForUnknownProject()
    {
        using var scope = _services.CreateScope();
        var metricsService = scope.ServiceProvider.GetRequiredService<IProjectMetricsService>();

        var metrics = await metricsService.GetMetricsForProjectsAsync(
            [
                new ProjectMetricRequest(
                    "nonexistent",
                    "sametcn99/this-repo-does-not-exist-zz9x",
                    "not-a-real-package-zz9x"
                ),
            ],
            "sametcn99"
        );

        Assert.NotNull(metrics);
        Assert.Null(metrics.GetValueOrDefault("nonexistent"));
    }

    [Fact]
    public async Task SkillsMarkdown_ContainsExpertisePillars()
    {
        var response = await _client.GetAsync("/skills.md");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var markdown = await response.Content.ReadAsStringAsync();
        Assert.Contains("## Expertise", markdown);
        Assert.Contains("Full-stack product engineering", markdown);
        Assert.Contains("/project/repotruck", markdown);
    }
}
