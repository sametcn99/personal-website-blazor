using personal_website_blazor.Presentation.Startup;

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
	?? Environments.Production;

var options = string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase)
	? new WebApplicationOptions
	{
		Args = args,
		WebRootPath = "src/wwwroot"
	}
	: new WebApplicationOptions
	{
		Args = args
	};

var builder = WebApplication.CreateBuilder(options);

if (builder.Environment.IsDevelopment())
{
	builder.WebHost.UseStaticWebAssets();
}
builder.ConfigureApplication();

var app = builder.Build();
app.UseApplicationPipeline();
app.MapApplicationEndpoints();

app.Run();
