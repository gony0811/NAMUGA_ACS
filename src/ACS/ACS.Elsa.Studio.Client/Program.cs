using System.Text.Json;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Options;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(new()
{
    ConfigureHttpClientBuilder = options =>
        options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler)
});
builder.Services.AddLoginModule();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();

var app = builder.Build();

var js = app.Services.GetRequiredService<IJSRuntime>();
var clientConfig = await js.InvokeAsync<JsonElement>("getClientConfig");
var apiUrl = clientConfig.GetProperty("apiUrl").GetString()
    ?? throw new InvalidOperationException("No API URL configured.");
app.Services.GetRequiredService<IOptions<BackendOptions>>().Value.Url = new(apiUrl);

var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

await app.RunAsync();
