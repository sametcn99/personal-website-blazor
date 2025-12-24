using MudBlazor.Services;
using personal_website_blazor.Components;

var builder = WebApplication.CreateBuilder(args);

// Disable reload on change to prevent inotify issues in Docker/Linux environments
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: false
);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<
    personal_website_blazor.Services.IContentService,
    personal_website_blazor.Services.ContentService
>();
builder.Services.AddScoped<
    personal_website_blazor.Services.IRssFeedService,
    personal_website_blazor.Services.RssFeedService
>();

builder.Services.AddScoped<
    personal_website_blazor.Services.ISitemapService,
    personal_website_blazor.Services.SitemapService
>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapGet(
    "/rss.xml",
    async (HttpContext context, personal_website_blazor.Services.IRssFeedService feedService) =>
    {
        var request = context.Request;
        var baseUri = new Uri($"{request.Scheme}://{request.Host}");

        var xml = await feedService.BuildFeedAsync(baseUri);

        context.Response.Headers.CacheControl = "public, max-age=1800";
        return Results.Content(xml, "application/rss+xml");
    }
);

app.MapGet(
    "/sitemap.xml",
    async (HttpContext context, personal_website_blazor.Services.ISitemapService sitemapService) =>
    {
        var request = context.Request;
        var baseUri = new Uri($"{request.Scheme}://{request.Host}");

        var xml = await sitemapService.GenerateSitemapXmlAsync(baseUri);

        context.Response.Headers.CacheControl = "public, max-age=86400";
        return Results.Content(xml, "application/xml");
    }
);

app.Run();
