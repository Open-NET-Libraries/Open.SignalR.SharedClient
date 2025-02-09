using Open.SignalR.SharedClient;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
services.AddRazorComponents()
	.AddInteractiveServerComponents()
	.AddInteractiveWebAssemblyComponents();

// Add SignalR Configuration
// https://learn.microsoft.com/en-us/aspnet/core/blazor/tutorials/signalr-blazor?view=aspnetcore-9.0&tabs=visual-studio
services.AddSignalR();
//services.AddResponseCompression(
//	opts => opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]));

services.AddSingleton<CountService>();

// Needs to be registered here so that WebAssembly doesn't freak out.
builder.Services
	.AddScopedHubConnectionProvider();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AddInteractiveWebAssemblyRenderMode()
	.AddAdditionalAssemblies(typeof(WebTest.Client._Imports).Assembly);

//app.UseResponseCompression();
app.MapHub<CounterHub>("/hub/counter");

await app.RunAsync();
