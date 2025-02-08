var builder = WebAssemblyHostBuilder
	.CreateDefault(args);

builder.Services
	.AddScopedHubConnectionProvider();

await builder
	.Build()
	.RunAsync();
