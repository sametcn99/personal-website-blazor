using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;
using personal_website_blazor.Services;

namespace personal_website_blazor.Controllers;

[ApiController]
[Route("/")]
public class MetadataController : ControllerBase
{
    private readonly IOptions<CachePolicyOptions> _cacheOptions;
    private readonly IMarkdownForAgentsService _markdownForAgentsService;
    private readonly IWebHostEnvironment _env;

    public MetadataController(
        IOptions<CachePolicyOptions> cacheOptions,
        IMarkdownForAgentsService markdownForAgentsService,
        IWebHostEnvironment env)
    {
        _cacheOptions = cacheOptions;
        _markdownForAgentsService = markdownForAgentsService;
        _env = env;
    }

    [HttpGet("llms.txt")]
    public async Task<ActionResult> GetLlmsTxt()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var llmsTxt = await _markdownForAgentsService.GetLlmsTxtAsync(baseUrl);

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.LlmsMaxAgeSeconds}";
        return Content(llmsTxt, "text/plain; charset=utf-8");
    }

    [HttpGet("llms-full.txt")]
    public async Task<ActionResult> GetLlmsFullTxt()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var llmsFullTxt = await _markdownForAgentsService.GetLlmsFullTxtAsync(baseUrl);

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.LlmsMaxAgeSeconds}";
        return Content(llmsFullTxt, "text/plain; charset=utf-8");
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
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/api/profile",
                    ["service-desc"] = new[] { new { href = $"{baseUrl}/openapi.json", type = "application/openapi+json" } },
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/about", type = "text/html" } },
                },
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/api/timeline",
                    ["service-desc"] = new[] { new { href = $"{baseUrl}/openapi.json", type = "application/openapi+json" } },
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/timeline", type = "text/html" } },
                },
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/api/skills",
                    ["service-desc"] = new[] { new { href = $"{baseUrl}/openapi.json", type = "application/openapi+json" } },
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/skills", type = "text/html" } },
                },
                new Dictionary<string, object>
                {
                    ["anchor"] = $"{baseUrl}/.well-known/mcp/server-card.json",
                    ["service-doc"] = new[] { new { href = $"{baseUrl}/llms.txt", type = "text/markdown" } },
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

    [HttpGet(".well-known/ai-catalog.json")]
    public ActionResult GetAiCatalog()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var catalog = new
        {
            specVersion = "1.0",
            host = new
            {
                displayName = "Samet Can Cıncık Personal Website",
                identifier = "sametcc.me",
                documentationUrl = $"{baseUrl}/llms.txt",
            },
            entries = new[]
            {
                new
                {
                    identifier = "urn:air:sametcc.me:mcp:personal-website",
                    displayName = "Personal Website MCP Server",
                    type = "application/mcp-server-card+json",
                    url = $"{baseUrl}/.well-known/mcp/server-card.json",
                    description = "Read-only MCP tools for discovering the public profile, content, projects, taxonomy, and related resources.",
                    representativeQueries = new[]
                    {
                        "find projects that use Blazor or .NET",
                        "search the author's technical writing about Docker",
                    },
                },
                new
                {
                    identifier = "urn:air:sametcc.me:a2a:personal-website",
                    displayName = "Personal Website A2A Agent",
                    type = "application/a2a-agent-card+json",
                    url = $"{baseUrl}/.well-known/agent-card.json",
                    description = "An agent for public profile, technical writing, project documentation, and repository discovery.",
                    representativeQueries = new[]
                    {
                        "tell me about Samet Can Cıncık's software development experience",
                        "find the author's project documentation about agent discovery",
                    },
                },
                new
                {
                    identifier = "urn:air:sametcc.me:api:public-openapi",
                    displayName = "Public Website HTTP API",
                    type = "application/json",
                    url = $"{baseUrl}/openapi.json",
                    description = "OpenAPI schema for the public profile, content, repository, and discovery HTTP endpoints.",
                    representativeQueries = new[]
                    {
                        "what HTTP endpoints expose the author's profile and skills",
                        "how can I search the site's public content API",
                    },
                },
                new
                {
                    identifier = "urn:air:sametcc.me:skill:website-content",
                    displayName = "Website Content Agent Skill",
                    type = "application/json",
                    url = $"{baseUrl}/.well-known/agent-skills/index.json",
                    description = "Discoverable skill metadata for reading and using the site's public content.",
                    representativeQueries = new[]
                    {
                        "how should an agent navigate this website's content",
                        "where can an agent find the full public context for this site",
                    },
                },
            },
        };

        Response.Headers["Access-Control-Allow-Origin"] = "*";
        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/json");
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

    [HttpGet(".well-known/http-message-signatures-directory")]
    public ActionResult GetHttpMessageSignaturesDirectory()
    {
        var directory = new
        {
            keys = new[]
            {
                new
                {
                    kty = "EC",
                    crv = "P-256",
                    alg = "ecdsa-p256-sha256",
                    use = "sig",
                    kid = "0FFqT73VjKb0pLWYRfpYhJSxa9xFwoYM5EwLpCbFMyk",
                    x = "92ftNJjulJPwDi0PD_ONad9HmQYD7npZhEr45JCbLtA",
                    y = "H3LBSSbNuVXp9PaA57tPVnb35TOBEf1s8MJ25Wm26SY",
                },
            },
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(directory, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/http-message-signatures-directory+json");
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
            revocation_endpoint = $"{baseUrl}/agent/revoke",
            agent_auth = new
            {
                skill = $"{baseUrl}/auth.md",
                register_uri = $"{baseUrl}/agent/register",
                identity_types_supported = new[] { "anonymous" },
                supported_identity_types = new[] { "anonymous" },
                credential_types_supported = new[] { "client_secret_basic", "client_secret_post", "bearer" },
                claim_uri = $"{baseUrl}/api/profile",
                revocation_uri = $"{baseUrl}/agent/revoke",
                anonymous = new
                {
                    credential_types_supported = new[] { "client_secret_basic", "client_secret_post", "bearer" },
                    claim_uri = $"{baseUrl}/api/profile",
                },
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

    [HttpGet(".well-known/agent-card.json")]
    public ActionResult GetA2aAgentCard()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var card = new
        {
            name = "Samet Can Cıncık Agent",
            version = "1.0.0",
            description = "An agent for discovering Samet Can Cıncık's public profile, technical writing, projects, and repository context.",
            supportedInterfaces = new[]
            {
                new
                {
                    url = $"{baseUrl}/a2a",
                    protocolBinding = "JSONRPC",
                    protocolVersion = "1.0.0",
                },
            },
            capabilities = new
            {
                streaming = false,
                pushNotifications = false,
            },
            skills = new[]
            {
                new
                {
                    id = "profile-discovery",
                    name = "Profile discovery",
                    description = "Discover the author's public identity, experience, education, skills, and professional timeline.",
                },
                new
                {
                    id = "content-discovery",
                    name = "Content discovery",
                    description = "Find and retrieve public blog posts, technical gists, project documentation, and repository context.",
                },
            },
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
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
                    digest = GetAgentSkillDigest(),
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

    [HttpGet(".well-known/mcp/server-card.json")]
    public ActionResult GetMcpServerCard()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var card = new
        {
            schema = "https://static.modelcontextprotocol.io/schemas/2025-10-17/server.schema.json",
            serverInfo = new
            {
                name = "io.sametcc.personal-website",
                version = "2.1.0",
            },
            name = "io.sametcc.personal-website",
            title = "Samet Can Cıncık Personal Website",
            description = "Agent-discoverable profile, technical writing, project documentation, and public repository context for Samet Can Cıncık.",
            websiteUrl = baseUrl,
            version = "2.1.0",
            transport = new
            {
                type = "streamable-http",
                endpoint = $"{baseUrl}/mcp",
            },
            remotes = new[]
            {
                new
                {
                    type = "streamable-http",
                    url = $"{baseUrl}/mcp",
                },
            },
            capabilities = new
            {
                tools = new { },
                resources = new { },
                prompts = new { },
            },
            discovery = new
            {
                profile = $"{baseUrl}/api/profile",
                content = $"{baseUrl}/api/content/all",
                repositories = $"{baseUrl}/api/profile/github",
                agentIndex = $"{baseUrl}/llms.txt",
            },
        };

        Response.Headers.CacheControl = $"public, max-age={_cacheOptions.Value.StaticAssetsMaxAgeSeconds}";
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        return Content(json, "application/json");
    }

    private string GetAgentSkillDigest()
    {
        var path = Path.Combine(_env.WebRootPath, ".well-known", "agent-skills", "website-content", "SKILL.md");
        if (!System.IO.File.Exists(path))
            return string.Empty;

        var hash = SHA256.HashData(System.IO.File.ReadAllBytes(path));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    [HttpGet("favicon.svg")]
    public ActionResult GetFavicon()
    {
        Response.Headers.CacheControl = "public, max-age=3600, stale-while-revalidate=86400";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Content(FaviconSvgBuilder.Build(), "image/svg+xml; charset=utf-8");
    }

    [HttpGet("favicon.ico")]
    public ActionResult GetFaviconIco() => RasterIcon(FaviconRasterizer.GetIco(), "image/x-icon");

    [HttpGet("favicon.png")]
    public ActionResult GetFaviconPng() => RasterIcon(FaviconRasterizer.GetPng(64), "image/png");

    [HttpGet("favicon-16x16.png")]
    public ActionResult GetFavicon16Png() => RasterIcon(FaviconRasterizer.GetPng(16), "image/png");

    [HttpGet("favicon-32x32.png")]
    public ActionResult GetFavicon32Png() => RasterIcon(FaviconRasterizer.GetPng(32), "image/png");

    [HttpGet("android-chrome-192x192.png")]
    public ActionResult GetAndroidChrome192Png() => RasterIcon(FaviconRasterizer.GetPng(192), "image/png");

    [HttpGet("android-chrome-512x512.png")]
    public ActionResult GetAndroidChrome512Png() => RasterIcon(FaviconRasterizer.GetPng(512), "image/png");

    [HttpGet("apple-touch-icon.png")]
    public ActionResult GetAppleTouchIconPng() => RasterIcon(FaviconRasterizer.GetPng(180), "image/png");

    private ActionResult RasterIcon(byte[] content, string contentType)
    {
        Response.Headers.CacheControl = "public, max-age=3600, stale-while-revalidate=86400";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(content, contentType);
    }

    [HttpGet("manifest.webmanifest")]
    public ActionResult GetManifest()
    {
        var manifest = new
        {
            name = "Samet Can Cıncık | Software Developer",
            short_name = "Samet Can",
            description = "Software Developer passionate about creating compelling and user-friendly software experiences.",
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
                new { src = "/favicon.svg?v=icons-v2", sizes = "any", type = "image/svg+xml", purpose = "any" },
                new { src = "/favicon-16x16.png?v=icons-v2", sizes = "16x16", type = "image/png", purpose = "any" },
                new { src = "/favicon-32x32.png?v=icons-v2", sizes = "32x32", type = "image/png", purpose = "any" },
                new { src = "/android-chrome-192x192.png?v=icons-v2", sizes = "192x192", type = "image/png", purpose = "any" },
                new { src = "/android-chrome-512x512.png?v=icons-v2", sizes = "512x512", type = "image/png", purpose = "any" },
                new { src = "/apple-touch-icon.png?v=icons-v2", sizes = "180x180", type = "image/png", purpose = "any" },
            },
            shortcuts = new object[]
            {
                new
                {
                    name = "Blog", short_name = "Blog", description = "View blog posts", url = "/blog",
                    icons = new[] { new { src = "/android-chrome-192x192.png?v=icons-v2", sizes = "192x192", type = "image/png" } },
                },
                new
                {
                    name = "Gists", short_name = "Gists", description = "View coding gists and tutorials", url = "/gist",
                    icons = new[] { new { src = "/android-chrome-192x192.png?v=icons-v2", sizes = "192x192", type = "image/png" } },
                },
                new
                {
                    name = "CV", short_name = "CV", description = "View curriculum vitae", url = "/cv",
                    icons = new[] { new { src = "/android-chrome-192x192.png?v=icons-v2", sizes = "192x192", type = "image/png" } },
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
        var svg = OpenGraphImageBuilder.Build(
            Request.Query["title"].ToString(),
            Request.Query["description"].ToString(),
            Request.Query["type"].ToString(),
            Request.Query["date"].ToString(),
            Request.Query["path"].ToString());

        Response.Headers.CacheControl = "public, max-age=900, stale-while-revalidate=86400";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Content(svg, "image/svg+xml; charset=utf-8");
    }
}
