using personal_website_blazor.Presentation.Startup;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
	builder.WebHost.UseStaticWebAssets();
}
builder.ConfigureApplication();

var app = builder.Build();
app.UseApplicationPipeline();
app.MapApplicationEndpoints();

app.Run();
