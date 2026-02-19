using System.Text.Json;
using Microsoft.Extensions.Options;
using personal_website_blazor.Core.Application.Configuration;

namespace personal_website_blazor.Presentation.Startup.Internal.Endpoints;

internal static class MetadataEndpointExtensions
{
    internal static WebApplication MapMetadataEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/manifest.webmanifest",
            (HttpContext context, IOptions<CachePolicyOptions> cacheOptions) =>
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
                        new
                        {
                            src = "/favicon-16x16.png",
                            sizes = "16x16",
                            type = "image/png",
                            purpose = "any",
                        },
                        new
                        {
                            src = "/favicon-32x32.png",
                            sizes = "32x32",
                            type = "image/png",
                            purpose = "any",
                        },
                        new
                        {
                            src = "/android-chrome-192x192.png",
                            sizes = "192x192",
                            type = "image/png",
                            purpose = "any",
                        },
                        new
                        {
                            src = "/android-chrome-512x512.png",
                            sizes = "512x512",
                            type = "image/png",
                            purpose = "any",
                        },
                        new
                        {
                            src = "/apple-touch-icon.png",
                            sizes = "180x180",
                            type = "image/png",
                            purpose = "any",
                        },
                    },
                    shortcuts = new object[]
                    {
                        new
                        {
                            name = "Blog",
                            short_name = "Blog",
                            description = "View blog posts",
                            url = "/blog",
                            icons = new[]
                            {
                                new
                                {
                                    src = "/android-chrome-192x192.png",
                                    sizes = "192x192",
                                    type = "image/png",
                                },
                            },
                        },
                        new
                        {
                            name = "Gists",
                            short_name = "Gists",
                            description = "View coding gists and tutorials",
                            url = "/gist",
                            icons = new[]
                            {
                                new
                                {
                                    src = "/android-chrome-192x192.png",
                                    sizes = "192x192",
                                    type = "image/png",
                                },
                            },
                        },
                        new
                        {
                            name = "CV",
                            short_name = "CV",
                            description = "View curriculum vitae",
                            url = "/cv",
                            icons = new[]
                            {
                                new
                                {
                                    src = "/android-chrome-192x192.png",
                                    sizes = "192x192",
                                    type = "image/png",
                                },
                            },
                        },
                    },
                    prefer_related_applications = false,
                    related_applications = Array.Empty<object>(),
                    dir = "ltr",
                };

                context.Response.Headers.CacheControl =
                    $"public, max-age={cacheOptions.Value.ManifestMaxAgeSeconds}, must-revalidate";
                var json = JsonSerializer.Serialize(manifest);
                return Results.Content(json, "application/manifest+json");
            }
        );

        app.MapGet(
            "/opengraph-image",
            () =>
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

                return Results.Content(svg, "image/svg+xml");
            }
        );

        return app;
    }
}
