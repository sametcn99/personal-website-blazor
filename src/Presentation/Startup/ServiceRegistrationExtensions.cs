using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using personal_website_blazor.Infrastructure;

namespace personal_website_blazor.Presentation.Startup;

public static class ServiceRegistrationExtensions
{
    public static WebApplicationBuilder ConfigureApplication(this WebApplicationBuilder builder)
    {
        ConfigureConfiguration(builder);
        ConfigureForwardedHeaders(builder.Services);
        ConfigureDataProtection(builder.Services, builder.Configuration, builder.Environment);
        RegisterApplicationServices(builder.Services, builder.Configuration);
        return builder;
    }

    private static void ConfigureConfiguration(WebApplicationBuilder builder)
    {
        // Disable reload on change to prevent inotify issues in Docker/Linux environments.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(
            "appsettings.json",
            optional: true,
            reloadOnChange: false
        );
        builder.Configuration.AddJsonFile(
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: false
        );
        builder.Configuration.AddEnvironmentVariables();
    }

    private static void ConfigureForwardedHeaders(IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;

            // This app runs behind container/reverse-proxy setups with dynamic upstream IPs.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    private static void ConfigureDataProtection(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("personal-website-blazor");

        var keysPath = configuration["DataProtection:KeysPath"];

        if (string.IsNullOrWhiteSpace(keysPath) && !environment.IsDevelopment())
        {
            keysPath = Path.Combine(environment.ContentRootPath, "data-protection-keys");
        }

        if (string.IsNullOrWhiteSpace(keysPath))
        {
            return;
        }

        Directory.CreateDirectory(keysPath);
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    }

    private static void RegisterApplicationServices(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();
        services.AddMudServices();

        services.AddInfrastructure(configuration);
    }
}
