using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using personal_website_blazor.Models;

namespace personal_website_blazor.Controllers;

[ApiController]
[Route("/")]
public class MetadataController : ControllerBase
{
    private readonly IOptions<CachePolicyOptions> _cacheOptions;

    public MetadataController(IOptions<CachePolicyOptions> cacheOptions)
    {
        _cacheOptions = cacheOptions;
    }

    [HttpGet(".well-known/api-catalog")]
    public ActionResult GetApiCatalog()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var catalog = new
        {
            linkset = new Dictionary<string, object>[]
            {
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/api/content/posts",
                    ["service-desc"] = new[] { new { href = $"{baseUrl}/openapi.json", type = "application/openapi+json" } },
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/blog", type = "text/html" } },
                },
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/api/content/gists",
                    ["service-desc"] = new[] { new { href = $"{baseUrl}/openapi.json", type = "application/openapi+json" } },
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/gist", type = "text/html" } },
                },
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/api/content/projects",
                    ["service-desc"] = new[] { new { href = $"{baseUrl}/openapi.json", type = "application/openapi+json" } },
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/project", type = "text/html" } },
                },
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/api/repos",
                    ["service-desc"] = new[] { new { href = $"{baseUrl}/openapi.json", type = "application/openapi+json" } },
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/repo", type = "text/html" } },
                },
            }
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/linkset+json; profile=\"https://www.rfc-editor.org/info/rfc9727\"");
    }

    [HttpGet(".well-known/oauth-protected-resource")]
    public ActionResult GetOAuthProtectedResource()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var metadata = new
        {
            resource = baseUrl,
            authorization_servers = new[] { $"{baseUrl}" },
            scopes_supported = new[] { "openid", "profile", "email" },
            bearer_methods_supported = new[] { "header", "query" },
            resource_name = "Samet Can Cıncık Personal Website",
            resource_documentation = $"{baseUrl}/auth.md",
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/json");
    }

    [HttpGet(".well-known/oauth-authorization-server")]
    public ActionResult GetOAuthAuthorizationServer()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var metadata = new
        {
            issuer = baseUrl,
            authorization_endpoint = $"{baseUrl}/agent/auth",
            token_endpoint = $"{baseUrl}/agent/token",
            registration_endpoint = $"{baseUrl}/agent/register",
            scopes_supported = new[] { "openid", "profile", "email" },
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "client_credentials" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" },
            service_documentation = $"{baseUrl}/auth.md",
            agent_auth = new
            {
                skill = $"{baseUrl}/auth.md",
                register_uri = $"{baseUrl}/agent/register",
                registration_methods = new[]
                {
                    new
                    {
                        type = "oauth2_dynamic_client_registration",
                        endpoint = $"{baseUrl}/agent/register",
                        method = "POST",
                        content_type = "application/json",
                        authentication = "none",
                    },
                },
            },
            revocation_endpoint = $"{baseUrl}/agent/revoke",
            events_supported = new[] { "urn:ietf:params:oauth:agent:registered", "urn:ietf:params:oauth:agent:revoked" },
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/json");
    }

    [HttpGet(".well-known/acp.json")]
    public ActionResult GetAcpDiscovery()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var discovery = new
        {
            protocol = new
            {
                name = "acp",
                version = "1.0.0",
            },
            api_base_url = $"{baseUrl}/api",
            transports = new[] { "https" },
            capabilities = new
            {
                services = new[]
                {
                    new { id = "content", name = "Content API", description = "Access blog posts, gists, and projects" },
                    new { id = "repos", name = "Repositories API", description = "Access GitHub repositories" },
                }
            },
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(discovery, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/json");
    }

    [HttpGet(".well-known/agent-skills/index.json")]
    public ActionResult GetAgentSkillsIndex()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var index = new Dictionary<string, object>
        {
            ["$schema"] = "https://schemas.agentskills.io/discovery/0.2.0/schema.json",
            ["skills"] = new[]
            {
                new
                {
                    name = "website-content",
                    type = "skill-md",
                    description = "Read and use the public blog posts, gists, projects, and repository content exposed by this website.",
                    url = $"{baseUrl}/.well-known/agent-skills/website-content/SKILL.md",
                    digest = "sha256:e0640ae2fc3783e4792ff3b7344c7d1ab5332cd9c098accd20531f055bd0cf95",
                },
            },
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(index, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/json");
    }

    [HttpGet("manifest.webmanifest")]
    public ActionResult GetManifest()
    {
        var manifest = new
        {
            name = "Samet Can Cıncık | Web Developer",
            short_name = "Samet Can",
            description = "Web Developer passionate about creating compelling and user-friendly web experiences.",
            start_url = "/",
            display = "standalone",
            background_color = "#0c0c0cff",
            theme_color = "#0c0c0cff",
            orientation = "portrait-primary",
            scope = "/",
            lang = "en",
            categories = new[] { "education", "productivity", "developer" },
            icons = new[]
            {
                new { src = "/favicon-16x16.png", sizes = "16x16", type = "image/png", purpose = "any" },
                new { src = "/favicon-32x32.png", sizes = "32x32", type = "image/png", purpose = "any" },
                new { src = "/android-chrome-192x192.png", sizes = "192x192", type = "image/png", purpose = "any" },
                new { src = "/android-chrome-512x512.png", sizes = "512x512", type = "image/png", purpose = "any" },
                new { src = "/apple-touch-icon.png", sizes = "180x180", type = "image/png", purpose = "any" },
            },
            shortcuts = new object[]
            {
                new
                {
                    name = "Blog", short_name = "Blog", description = "View blog posts", url = "/blog",
                    icons = new[] { new { src = "/android-chrome-192x192.png", sizes = "192x192", type = "image/png" } },
                },
                new
                {
                    name = "Gists", short_name = "Gists", description = "View coding gists and tutorials", url = "/gist",
                    icons = new[] { new { src = "/android-chrome-192x192.png", sizes = "192x192", type = "image/png" } },
                },
                new
                {
                    name = "CV", short_name = "CV", description = "View curriculum vitae", url = "/cv",
                    icons = new[] { new { src = "/android-chrome-192x192.png", sizes = "192x192", type = "image/png" } },
                },
            },
            prefer_related_applications = false,
            related_applications = Array.Empty<object>(),
            dir = "ltr",
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.ManifestMaxAgeSeconds}, must-revalidate";
        var json = JsonSerializer.Serialize(manifest);
        return Content(json, "application/manifest+json");
    }

    [HttpGet("opengraph-image")]
    public ActionResult GetOpenGraphImage()
    {
        const string svg = """
<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
    <defs>
        <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stop-color="#0c0c0c" />
            <stop offset="100%" stop-color="#151515" />
        </linearGradient>
    </defs>
    <rect width="1200" height="630" fill="url(#bg)" />
    <text x="80" y="270" fill="#ffffff" font-size="64" font-family="Arial, Helvetica, sans-serif" font-weight="700">Samet Can Cıncık</text>
    <text x="80" y="340" fill="#90caf9" font-size="40" font-family="Arial, Helvetica, sans-serif">Web Developer</text>
    <text x="80" y="410" fill="#b0b0b0" font-size="28" font-family="Arial, Helvetica, sans-serif">sametcc.me</text>
</svg>
""";
        return Content(svg, "image/svg+xml");
    }
}
