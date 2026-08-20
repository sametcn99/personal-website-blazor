using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace personal_website_blazor.IntegrationTests;

public sealed class McpControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public McpControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ToolsList_ExposesTimelineOutputAsAnObjectSchema()
    {
        using var response = await _client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list",
            @params = new { },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var timelineTool = document.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "get_timeline");
        var outputSchema = timelineTool.GetProperty("outputSchema");

        Assert.Equal("object", outputSchema.GetProperty("type").GetString());
        var itemsSchema = outputSchema
            .GetProperty("properties")
            .GetProperty("items");
        Assert.Equal("array", itemsSchema.GetProperty("type").GetString());
        Assert.Equal("object", itemsSchema.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public async Task ToolsCall_ReturnsTimelineInsideStructuredContentObject()
    {
        using var response = await _client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "get_timeline",
                arguments = new { },
            },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var structuredContent = document.RootElement
            .GetProperty("result")
            .GetProperty("structuredContent");

        Assert.Equal(JsonValueKind.Object, structuredContent.ValueKind);
        var items = structuredContent.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.NotEmpty(items.EnumerateArray());

        var firstItem = items.EnumerateArray().First();
        Assert.Equal(JsonValueKind.Object, firstItem.ValueKind);
        Assert.True(firstItem.TryGetProperty("type", out _));
        Assert.True(firstItem.TryGetProperty("organization", out _));
        Assert.True(firstItem.TryGetProperty("role", out _));
        Assert.True(firstItem.TryGetProperty("highlights", out _));
    }
}
