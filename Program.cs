using personal_website_blazor.Presentation.Startup;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	Args = args,
	WebRootPath = "src/wwwroot"
});

builder.WebHost.UseStaticWebAssets();
builder.ConfigureApplication();

var app = builder.Build();
app.UseApplicationPipeline();
app.MapApplicationEndpoints();

app.Run();
