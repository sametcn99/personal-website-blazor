namespace personal_website_blazor.Presentation.Startup.Internal.Pipeline;

internal static class CorePipelineExtensions
{
    internal static WebApplication UseCorePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseAntiforgery();

        return app;
    }
}
