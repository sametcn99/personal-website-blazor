using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using personal_website_blazor.Interfaces;
using personal_website_blazor.Models;

namespace personal_website_blazor.Controllers;

[ApiController]
[Route("mcp")]
public sealed class McpController : ControllerBase
{
    private const string ServerName = "io.sametcc.personal-website";
    private const string ServerVersion = "2.1.0";
    private readonly IContentService _contentService;
    private readonly IProfileService _profileService;
    private readonly IMarkdownForAgentsService _markdownForAgentsService;
    private readonly IMcpQueryService _queryService;

    public McpController(
        IContentService contentService,
        IProfileService profileService,
        IMarkdownForAgentsService markdownForAgentsService,
        IMcpQueryService queryService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _markdownForAgentsService = markdownForAgentsService;
        _queryService = queryService;
    }

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] JsonElement request)
    {
        if (!request.TryGetProperty("method", out var methodElement)
            || methodElement.ValueKind != JsonValueKind.String)
        {
            return JsonRpcError(GetRequestId(request), -32600, "Invalid JSON-RPC request.");
        }

        var method = methodElement.GetString();
        var requestId = GetRequestId(request);

        if (!requestId.HasValue && method?.StartsWith("notifications/", StringComparison.Ordinal) == true)
            return NoContent();

        return method switch
        {
            "initialize" => JsonRpcResult(requestId, new
            {
                protocolVersion = "2025-06-18",
                capabilities = new
                {
                    tools = new { listChanged = false },
                    resources = new { subscribe = false, listChanged = false },
                    prompts = new { listChanged = false },
                },
                serverInfo = new { name = ServerName, version = ServerVersion },
                instructions = "Use typed tools for profile, timeline, skills, projects, content, taxonomy, and related-content discovery. Use resources for the full public context.",
            }),
            "ping" => JsonRpcResult(requestId, new { }),
            "tools/list" => JsonRpcResult(requestId, new { tools = BuildTools() }),
            "tools/call" => await CallToolAsync(requestId, request),
            "resources/list" => JsonRpcResult(requestId, new
            {
                resources = new[]
                {
                    new { uri = "sametcc://profile", name = "Author Profile", description = "Structured professional profile for Samet Can Cıncık.", mimeType = "application/json" },
                    new { uri = "sametcc://content", name = "Published Content Index", description = "All public blog, gist, and project metadata.", mimeType = "application/json" },
                    new { uri = "sametcc://llms.txt", name = "LLM Navigation Index", description = "Concise agent navigation index.", mimeType = "text/plain" },
                    new { uri = "sametcc://llms-full.txt", name = "Full Agent Context", description = "Full profile and published content context.", mimeType = "text/plain" },
                },
            }),
            "resources/read" => await ReadResourceAsync(requestId, request),
            _ => JsonRpcError(requestId, -32601, $"Method '{method}' is not supported."),
        };
    }

    private async Task<IActionResult> CallToolAsync(JsonElement? requestId, JsonElement request)
    {
        try
        {
            if (!TryGetParameters(request, out var parameters)
                || !parameters.TryGetProperty("name", out var nameElement)
                || nameElement.ValueKind != JsonValueKind.String)
            {
                return JsonRpcError(requestId, -32602, "tools/call requires a tool name.");
            }

            var arguments = parameters.TryGetProperty("arguments", out var argumentsElement)
                && argumentsElement.ValueKind == JsonValueKind.Object
                ? argumentsElement
                : default;
            var name = nameElement.GetString();

            return name switch
            {
                "get_profile" => await GetProfileToolAsync(requestId),
                "list_projects" => await ListProjectsToolAsync(requestId, arguments),
                "get_content" => await GetContentToolAsync(requestId, arguments),
                "search_content" => await SearchContentToolAsync(requestId, arguments),
                "get_timeline" => await GetTimelineToolAsync(requestId),
                "get_skills" => await GetSkillsToolAsync(requestId),
                "list_taxonomy" => await ListTaxonomyToolAsync(requestId, arguments),
                "get_related_content" => await GetRelatedContentToolAsync(requestId, arguments),
                _ => JsonRpcError(requestId, -32602, $"Unknown tool '{name}'."),
            };
        }
        catch (ArgumentException ex)
        {
            return JsonRpcError(requestId, -32602, ex.Message);
        }
    }

    private async Task<IActionResult> GetProfileToolAsync(JsonElement? requestId)
    {
        var profile = await _queryService.GetProfileAsync();
        return ToolResult(requestId, profile, $"Public profile for {profile.Name}.");
    }

    private async Task<IActionResult> ListProjectsToolAsync(JsonElement? requestId, JsonElement arguments)
    {
        var result = await _queryService.ListProjectsAsync(new McpProjectFilter
        {
            Query = GetOptionalString(arguments, "query"),
            Tags = GetStringArray(arguments, "tags"),
            Technologies = GetStringArray(arguments, "technologies"),
            Topics = GetStringArray(arguments, "topics"),
            Language = GetOptionalString(arguments, "language"),
            Limit = GetOptionalInt(arguments, "limit", 20),
            Cursor = GetOptionalString(arguments, "cursor"),
        });
        return ToolResult(requestId, result, $"Found {result.Total} matching projects.");
    }

    private async Task<IActionResult> GetContentToolAsync(JsonElement? requestId, JsonElement arguments)
    {
        var section = GetRequiredString(arguments, "section");
        var slug = GetRequiredString(arguments, "slug");
        var includeBody = GetOptionalBoolean(arguments, "includeBody", true);
        var includeRelated = GetOptionalBoolean(arguments, "includeRelated", true);
        var result = await _queryService.GetContentAsync(section, slug, includeBody, includeRelated);
        return result is null
            ? JsonRpcError(requestId, -32002, "Content not found.")
            : ToolResult(requestId, result, $"Content document: {result.Title}.");
    }

    private async Task<IActionResult> SearchContentToolAsync(JsonElement? requestId, JsonElement arguments)
    {
        var result = await _queryService.SearchContentAsync(new McpSearchFilter
        {
            Query = GetRequiredString(arguments, "query"),
            Sections = GetStringArray(arguments, "sections"),
            Types = GetStringArray(arguments, "types"),
            Languages = GetStringArray(arguments, "languages"),
            Tags = GetStringArray(arguments, "tags"),
            Technologies = GetStringArray(arguments, "technologies"),
            Topics = GetStringArray(arguments, "topics"),
            Status = GetOptionalString(arguments, "status"),
            Limit = GetOptionalInt(arguments, "limit", 20),
            Cursor = GetOptionalString(arguments, "cursor"),
        });
        return ToolResult(requestId, result, $"Found {result.Total} matching content items.");
    }

    private async Task<IActionResult> GetTimelineToolAsync(JsonElement? requestId)
    {
        var result = await _queryService.GetTimelineAsync();
        return ToolResult(requestId, result, $"Found {result.Count} timeline entries.");
    }

    private async Task<IActionResult> GetSkillsToolAsync(JsonElement? requestId)
    {
        var result = await _queryService.GetSkillsAsync();
        return ToolResult(requestId, result, $"Found {result.Skills.Length} skills.");
    }

    private async Task<IActionResult> ListTaxonomyToolAsync(JsonElement? requestId, JsonElement arguments)
    {
        var result = await _queryService.GetTaxonomyAsync(GetStringArray(arguments, "sections"));
        return ToolResult(requestId, result, "Available content taxonomy values.");
    }

    private async Task<IActionResult> GetRelatedContentToolAsync(JsonElement? requestId, JsonElement arguments)
    {
        var result = await _queryService.GetRelatedContentAsync(
            GetRequiredString(arguments, "section"),
            GetRequiredString(arguments, "slug"),
            GetOptionalInt(arguments, "limit", 10));

        return result is null
            ? JsonRpcError(requestId, -32002, "Content not found.")
            : ToolResult(requestId, result, $"Related content for {result.Source.Title}.");
    }

    private async Task<IActionResult> ReadResourceAsync(JsonElement? requestId, JsonElement request)
    {
        if (!TryGetParameters(request, out var parameters)
            || !parameters.TryGetProperty("uri", out var uriElement)
            || uriElement.ValueKind != JsonValueKind.String)
        {
            return JsonRpcError(requestId, -32602, "resources/read requires a URI.");
        }

        var uri = uriElement.GetString();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        string? text = uri switch
        {
            "sametcc://profile" => JsonSerializer.Serialize(await _profileService.GetProfileAsync()),
            "sametcc://content" => JsonSerializer.Serialize(await _contentService.GetAllContentsAsync()),
            "sametcc://llms.txt" => await _markdownForAgentsService.GetLlmsTxtAsync(baseUrl),
            "sametcc://llms-full.txt" => await _markdownForAgentsService.GetLlmsFullTxtAsync(baseUrl),
            _ => null,
        };

        return text is null
            ? JsonRpcError(requestId, -32002, "Resource not found.")
            : JsonRpcResult(requestId, new
            {
                contents = new[]
                {
                    new
                    {
                        uri,
                        mimeType = uri?.EndsWith(".txt", StringComparison.Ordinal) == true ? "text/plain" : "application/json",
                        text,
                    },
                },
            });
    }

    private static object[] BuildTools() =>
    [
        Tool("get_profile", "Get Profile", "Return the author's complete public profile, experience, education, skills, links, and certifications.", EmptySchema(), ObjectSchema(), ReadOnlyAnnotations()),
        Tool("list_projects", "List Projects", "List public projects with optional query, technology, topic, tag, language, and pagination filters.", ProjectSchema(), PageSchema(), ReadOnlyAnnotations()),
        Tool("get_content", "Get Content", "Get one public post, gist, or project by section and slug.", ContentSchema(), ObjectSchema(), ReadOnlyAnnotations()),
        Tool("search_content", "Search Content", "Search public content and filter results by section, type, language, tags, technologies, topics, status, and pagination.", SearchSchema(), PageSchema(), ReadOnlyAnnotations()),
        Tool("get_timeline", "Get Timeline", "Return public work experience and education timeline entries.", EmptySchema(), ArraySchema(), ReadOnlyAnnotations()),
        Tool("get_skills", "Get Skills", "Return public technical skills, areas of interest, languages, and public notes.", EmptySchema(), ObjectSchema(), ReadOnlyAnnotations()),
        Tool("list_taxonomy", "List Taxonomy", "Return available tags, technologies, topics, content types, and languages with usage counts.", TaxonomySchema(), ObjectSchema(), ReadOnlyAnnotations()),
        Tool("get_related_content", "Get Related Content", "Resolve related project and post metadata for one public content item.", RelatedSchema(), ObjectSchema(), ReadOnlyAnnotations()),
    ];

    private static object Tool(string name, string title, string description, object inputSchema, object outputSchema, object annotations) =>
        new { name, title, description, inputSchema, outputSchema, annotations };

    private static object ReadOnlyAnnotations() => new
    {
        readOnlyHint = true,
        destructiveHint = false,
        idempotentHint = true,
        openWorldHint = false,
    };

    private static object EmptySchema() => new { type = "object", properties = new { } };
    private static object ObjectSchema() => new { type = "object" };
    private static object ArraySchema() => new { type = "array" };
    private static object PageSchema() => new { type = "object", properties = new { items = new { type = "array" }, total = new { type = "integer" }, nextCursor = new { type = new[] { "string", "null" } } } };

    private static object ProjectSchema() => new
    {
        type = "object",
        properties = new
        {
            query = new { type = "string" },
            tags = StringArraySchema(),
            technologies = StringArraySchema(),
            topics = StringArraySchema(),
            language = new { type = "string", @enum = new[] { "en", "tr" } },
            limit = new { type = "integer", minimum = 1, maximum = 50, @default = 20 },
            cursor = new { type = "string" },
        },
    };

    private static object SearchSchema() => new
    {
        type = "object",
        required = new[] { "query" },
        properties = new
        {
            query = new { type = "string", minLength = 1 },
            sections = StringArraySchema(new[] { "posts", "gists", "projects" }),
            types = StringArraySchema(new[] { "post", "gist", "project" }),
            languages = StringArraySchema(new[] { "en", "tr" }),
            tags = StringArraySchema(),
            technologies = StringArraySchema(),
            topics = StringArraySchema(),
            status = new { type = "string", @enum = new[] { "published" } },
            limit = new { type = "integer", minimum = 1, maximum = 50, @default = 20 },
            cursor = new { type = "string" },
        },
    };

    private static object ContentSchema() => new
    {
        type = "object",
        required = new[] { "section", "slug" },
        properties = new
        {
            section = new { type = "string", @enum = new[] { "posts", "gists", "projects", "blog", "gist", "project" } },
            slug = new { type = "string" },
            includeBody = new { type = "boolean", @default = true },
            includeRelated = new { type = "boolean", @default = true },
        },
    };

    private static object RelatedSchema() => new
    {
        type = "object",
        required = new[] { "section", "slug" },
        properties = new
        {
            section = new { type = "string", @enum = new[] { "posts", "gists", "projects", "blog", "gist", "project" } },
            slug = new { type = "string" },
            limit = new { type = "integer", minimum = 1, maximum = 50, @default = 10 },
        },
    };

    private static object TaxonomySchema() => new
    {
        type = "object",
        properties = new
        {
            sections = StringArraySchema(new[] { "posts", "gists", "projects" }),
        },
    };

    private static object StringArraySchema(string[]? values = null) => values is null
        ? new { type = "array", items = new { type = "string" } }
        : new { type = "array", items = new { type = "string", @enum = values } };

    private static string GetRequiredString(JsonElement arguments, string name)
    {
        var value = GetOptionalString(arguments, name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{name} is required.")
            : value;
    }

    private static string? GetOptionalString(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object
        && arguments.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string[] GetStringArray(JsonElement arguments, string name)
    {
        if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty(name, out var value))
            return Array.Empty<string>();
        if (value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"{name} must be an array of strings.");
        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToArray();
    }

    private static int GetOptionalInt(JsonElement arguments, string name, int defaultValue)
    {
        if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty(name, out var value))
            return defaultValue;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result)
            ? result
            : throw new ArgumentException($"{name} must be an integer.");
    }

    private static bool GetOptionalBoolean(JsonElement arguments, string name, bool defaultValue)
    {
        if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty(name, out var value))
            return defaultValue;
        return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False
            ? value.GetBoolean()
            : throw new ArgumentException($"{name} must be a boolean.");
    }

    private static bool TryGetParameters(JsonElement request, out JsonElement parameters)
    {
        if (request.TryGetProperty("params", out parameters) && parameters.ValueKind == JsonValueKind.Object)
            return true;

        parameters = default;
        return false;
    }

    private static JsonElement? GetRequestId(JsonElement request) =>
        request.TryGetProperty("id", out var id) ? id : null;

    private static IActionResult ToolResult(JsonElement? requestId, object structuredContent, string text) =>
        JsonRpcResult(requestId, new
        {
            content = new[] { new { type = "text", text } },
            structuredContent,
        });

    private static IActionResult JsonRpcResult(JsonElement? requestId, object result) =>
        new JsonResult(new { jsonrpc = "2.0", id = requestId, result });

    private static IActionResult JsonRpcError(JsonElement? requestId, int code, string message) =>
        new JsonResult(new
        {
            jsonrpc = "2.0",
            id = requestId,
            error = new { code, message },
        });
}
