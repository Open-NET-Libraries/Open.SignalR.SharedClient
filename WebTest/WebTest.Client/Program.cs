using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using WebTest.Client;

var builder = WebAssemblyHostBuilder
	.CreateDefault(args);

builder.Services.AddAppHubConnections();

await builder
	.Build()
	.RunAsync();
