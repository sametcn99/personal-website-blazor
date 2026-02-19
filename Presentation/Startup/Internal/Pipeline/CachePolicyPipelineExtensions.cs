using Microsoft.Extensions.Options;
using personal_website_blazor.Core.Application.Configuration;

namespace personal_website_blazor.Presentation.Startup.Internal.Pipeline;

internal static class CachePolicyPipelineExtensions
{
    internal static WebApplication UseCachePolicyPipeline(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<CachePolicyOptions>>().Value;

        app.Use(
            async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var path = context.Request.Path;

                    // Keep the interactive Blazor circuit endpoint fully non-cacheable.
                    if (path.StartsWithSegments("/_blazor"))
                    {
                        context.Response.Headers.CacheControl =
                            "no-store, no-cache, must-revalidate";
                        context.Response.Headers.Pragma = "no-cache";
                        context.Response.Headers.Expires = "0";
                        return Task.CompletedTask;
                    }

                    // Preserve explicit cache policies set by dynamic endpoints.
                    if (
                        path.Equals("/rss.xml", StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/feed.json", StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/manifest.webmanifest", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        return Task.CompletedTask;
                    }

                    // Apply a short-lived browser cache for static assets.
                    if (
                        context.Response.StatusCode == StatusCodes.Status200OK
                        && IsStaticAssetPath(path)
                    )
                    {
                        context.Response.Headers.CacheControl =
                            $"public, max-age={options.StaticAssetsMaxAgeSeconds}";
                        context.Response.Headers.Pragma = string.Empty;
                        context.Response.Headers.Expires = string.Empty;
                    }

                    return Task.CompletedTask;
                });

                await next();
            }
        );

        return app;
    }

    private static bool IsStaticAssetPath(PathString path)
    {
        if (!Path.HasExtension(path))
        {
            return false;
        }

        var value = path.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var extension = Path.GetExtension(value);
        return extension.Equals(".css", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".js", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mjs", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".map", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".svg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ico", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".woff", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".woff2", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".eot", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }
}
