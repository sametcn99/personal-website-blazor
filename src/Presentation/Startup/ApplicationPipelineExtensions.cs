using personal_website_blazor.Presentation.Startup.Internal.Pipeline;

namespace personal_website_blazor.Presentation.Startup;

public static class ApplicationPipelineExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseCorePipeline();
        app.UseCachePolicyPipeline();
        app.MapRazorUi();

        return app;
    }
}
