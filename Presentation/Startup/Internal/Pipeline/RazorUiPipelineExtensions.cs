using personal_website_blazor.Components;

namespace personal_website_blazor.Presentation.Startup.Internal.Pipeline;

internal static class RazorUiPipelineExtensions
{
    internal static WebApplication MapRazorUi(this WebApplication app)
    {
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode();
        return app;
    }
}
