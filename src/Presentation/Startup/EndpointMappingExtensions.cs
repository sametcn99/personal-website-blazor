using personal_website_blazor.Presentation.Startup.Internal.Endpoints;

namespace personal_website_blazor.Presentation.Startup;

public static class EndpointMappingExtensions
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapSyndicationEndpoints();
        app.MapMetadataEndpoints();
        app.MapContentApiEndpoints();
        return app;
    }
}
