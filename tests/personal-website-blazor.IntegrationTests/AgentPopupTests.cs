using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace personal_website_blazor.IntegrationTests;

public sealed class AgentPopupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;

    public AgentPopupTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _services = factory.Services;
    }

    [Fact]
    public async Task HomePage_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("footer-copy-prompt-click", html);
    }

    [Fact]
    public void AgentPopup_LocalizationResources_AreResolvable()
    {
        using var scope = _services.CreateScope();
        var localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<SharedResources>>();

        // Test English
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var enTitle = localizer["AgentPopup_Title"];
        var enReport = localizer["AgentPopup_ReportLink"];
        var enCopyPrompt = localizer["AgentPopup_CopyPrompt"];
        var enPrompt = localizer["AgentPopup_AgentPromptTemplate", "https://sametcc.me", "https://sametcc.me"];

        Assert.False(enTitle.ResourceNotFound);
        Assert.Equal("Optimized for AI Agents", enTitle.Value);
        Assert.False(enReport.ResourceNotFound);
        Assert.False(enCopyPrompt.ResourceNotFound);
        Assert.Equal("Copy Prompt", enCopyPrompt.Value);
        Assert.False(enPrompt.ResourceNotFound);
        Assert.Contains("Research and analyze Samet Can Cıncık's personal website", enPrompt.Value);
        Assert.Contains("## 2. Research & Execution Instructions", enPrompt.Value);
        Assert.Contains("Guided Site Discovery", enPrompt.Value);
        Assert.Contains("https://sametcc.me/blog/{slug}.md", enPrompt.Value);

        // Test Turkish (unified detailed prompt template)
        CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
        var trTitle = localizer["AgentPopup_Title"];
        var trReport = localizer["AgentPopup_ReportLink"];
        var trCopyPrompt = localizer["AgentPopup_CopyPrompt"];
        var trPrompt = localizer["AgentPopup_AgentPromptTemplate", "https://sametcc.me", "https://sametcc.me"];

        Assert.False(trTitle.ResourceNotFound);
        Assert.Equal("Yapay Zeka Ajanları İçin Optimize", trTitle.Value);
        Assert.False(trReport.ResourceNotFound);
        Assert.False(trCopyPrompt.ResourceNotFound);
        Assert.Equal("Promptu Kopyala", trCopyPrompt.Value);
        Assert.False(trPrompt.ResourceNotFound);
        Assert.Contains("Research and analyze Samet Can Cıncık's personal website", trPrompt.Value);
        Assert.Contains("## 2. Research & Execution Instructions", trPrompt.Value);
        Assert.Contains("Guided Site Discovery", trPrompt.Value);
        Assert.Contains("https://sametcc.me/blog/{slug}.md", trPrompt.Value);
    }

    [Fact]
    public async Task SkillsApi_ContainsNewlyAddedSkills()
    {
        var response = await _client.GetAsync("/api/skills");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var skills = json.RootElement.GetProperty("skills").EnumerateArray().Select(s => s.GetString()).ToList();

        var expectedSkills = new[]
        {
            "Jest",
            "Redux",
            "Redux Toolkit",
            "Zustand",
            "MUI",
            "Tailwind CSS",
            "TimescaleDB",
            "InfluxDB",
            "SEO",
            "AEO",
            "Google Ads",
            "Prometheus",
            "Grafana"
        };

        foreach (var expected in expectedSkills)
        {
            Assert.Contains(expected, skills);
        }

        var areasOfInterest = json.RootElement.GetProperty("areasOfInterest").EnumerateArray().Select(s => s.GetString()).ToList();
        Assert.Contains("Digital marketing", areasOfInterest);
    }
}
