using System.Text;
using personal_website_blazor.Interfaces;

namespace personal_website_blazor.Middleware;

public class MarkdownForAgentsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MarkdownForAgentsMiddleware> _logger;

    public MarkdownForAgentsMiddleware(RequestDelegate next, ILogger<MarkdownForAgentsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldConvertToMarkdown(context.Request))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        var markdownService = context.RequestServices.GetRequiredService<IMarkdownForAgentsService>();
        var markdown = await markdownService.GetPageMarkdownAsync(path);

        if (markdown is null)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/markdown; charset=utf-8";
        context.Response.Headers.Vary = "Accept";
        context.Response.Headers.CacheControl = "public, max-age=3600";

        var tokenCount = await markdownService.EstimateTokenCountAsync(markdown);
        context.Response.Headers["x-markdown-tokens"] = tokenCount.ToString();

        var markdownBytes = Encoding.UTF8.GetBytes(markdown);
        context.Response.Headers.ContentLength = markdownBytes.Length;

        await context.Response.Body.WriteAsync(markdownBytes);
    }

    private static bool ShouldConvertToMarkdown(HttpRequest request)
    {
        if (request.Method != HttpMethods.Get)
            return false;

        var acceptHeader = request.Headers.Accept.ToString();
        if (string.IsNullOrEmpty(acceptHeader))
            return false;

        return acceptHeader.Contains("text/markdown", StringComparison.OrdinalIgnoreCase);
    }
}
