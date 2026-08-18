using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using personal_website_blazor.Interfaces;

namespace personal_website_blazor.Controllers;

[ApiController]
[Route("mcp")]
public sealed class McpController : ControllerBase
{
    private const string ServerName = "io.sametcc.personal-website";
    private const string ServerVersion = "2.0.0";
    private readonly IContentService _contentService;
    private readonly IProfileService _profileService;
    private readonly IMarkdownForAgentsService _markdownForAgentsService;

    public McpController(
        IContentService contentService,
        IProfileService profileService,
        IMarkdownForAgentsService markdownForAgentsService)
    {
        _contentService = contentService;
        _profileService = profileService;
        _markdownForAgentsService = markdownForAgentsService;
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
                instructions = "Use the profile resource for author identity, search_content for published material, and the llms resources for broader context.",
            }),
            "ping" => JsonRpcResult(requestId, new { }),
            "tools/list" => JsonRpcResult(requestId, new
            {
                tools = new[]
                {
                    new
                    {
                        name = "search_content",
                        description = "Search Samet Can Cıncık's public blog posts, technical gists, and projects.",
                        inputSchema = new
                        {
                            type = "object",
                            required = new[] { "query" },
                            properties = new
                            {
                                query = new { type = "string", description = "Search query." },
                                section = new { type = "string", @enum = new[] { "posts", "gists", "projects" }, description = "Optional content section filter." },
                            },
                        },
                    },
                },
            }),
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
        if (!TryGetParameters(request, out var parameters)
            || !parameters.TryGetProperty("name", out var nameElement)
            || nameElement.GetString() != "search_content")
        {
            return JsonRpcError(requestId, -32602, "The search_content tool is required.");
        }

        if (!parameters.TryGetProperty("arguments", out var arguments)
            || !arguments.TryGetProperty("query", out var queryElement)
            || queryElement.ValueKind != JsonValueKind.String)
        {
            return JsonRpcError(requestId, -32602, "search_content requires a query argument.");
        }

        var query = queryElement.GetString() ?? string.Empty;
        var section = arguments.TryGetProperty("section", out var sectionElement)
            ? sectionElement.GetString()
            : null;
        var results = query.Length > 200
            ? Array.Empty<object>()
            : (await _contentService.SearchAsync(query, section)).Cast<object>().ToArray();

        return JsonRpcResult(requestId, new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(results),
                },
            },
        });
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

    private static bool TryGetParameters(JsonElement request, out JsonElement parameters)
    {
        if (request.TryGetProperty("params", out parameters) && parameters.ValueKind == JsonValueKind.Object)
            return true;

        parameters = default;
        return false;
    }

    private static JsonElement? GetRequestId(JsonElement request) =>
        request.TryGetProperty("id", out var id) ? id : null;

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
