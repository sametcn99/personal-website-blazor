using Microsoft.AspNetCore.Mvc;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Controllers;

[ApiController]
[Route("api")]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IGitHubService _gitHubService;

    public ProfileController(IProfileService profileService, IGitHubService gitHubService)
    {
        _profileService = profileService;
        _gitHubService = gitHubService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ProfileDocument>> GetProfile()
    {
        var profile = await _profileService.GetProfileAsync();
        return Ok(profile);
    }

    [HttpGet("about")]
    public Task<ActionResult<ProfileDocument>> GetAbout() => GetProfile();

    [HttpGet("timeline")]
    public async Task<ActionResult<IEnumerable<TimelineEntry>>> GetTimeline()
    {
        var profile = await _profileService.GetProfileAsync();
        var timeline = profile.Experience
            .Select(item => new TimelineEntry
            {
                Type = "experience",
                Organization = item.Organization,
                Role = item.Role,
                Location = item.Location,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Highlights = item.Highlights,
            })
            .Concat(profile.Education.Select(item => new TimelineEntry
            {
                Type = "education",
                Organization = item.Institution,
                Role = item.Program,
                Level = item.Level,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Highlights = item.Highlights,
            }))
            .OrderByDescending(item => item.StartDate)
            .ToList();

        return Ok(timeline);
    }

    [HttpGet("skills")]
    public async Task<ActionResult> GetSkills()
    {
        var profile = await _profileService.GetProfileAsync();
        return Ok(new
        {
            skills = profile.Skills,
            areasOfInterest = profile.AreasOfInterest,
            languages = profile.Languages,
            publicNotes = profile.PublicNotes,
            source = "/api/profile",
            lastUpdated = profile.LastUpdated,
        });
    }

    [HttpGet("profile/github")]
    public async Task<ActionResult<GitHubProfileDocument>> GetGitHubProfile()
    {
        try
        {
            var profile = await _gitHubService.GetUserProfileAsync("sametcn99");
            var repositories = await _gitHubService.GetUserRepositoriesAsync("sametcn99");
            return Ok(new GitHubProfileDocument
            {
                Profile = profile,
                Repositories = repositories,
                RetrievedAt = DateTimeOffset.UtcNow.ToString("O"),
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "GitHub profile data is temporarily unavailable.",
                source = "https://github.com/sametcn99",
            });
        }
    }
}
