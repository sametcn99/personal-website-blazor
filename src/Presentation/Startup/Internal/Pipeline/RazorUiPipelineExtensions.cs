using personal_website_blazor.Components;
using personal_website_blazor.Components.Features.Status;
using personal_website_blazor.Client;

namespace personal_website_blazor.Presentation.Startup.Internal.Pipeline;

internal static class RazorUiPipelineExtensions
{
    internal static WebApplication MapRazorUi(this WebApplication app)
    {
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(ClientAssemblyMarker).Assembly, typeof(NotFound).Assembly);
        return app;
    }
}
