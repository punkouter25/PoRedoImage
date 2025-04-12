using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Client;
using Client.Services;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure regular HttpClient for public/anonymous endpoints
// Explicitly set the BaseAddress to the server's URL
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7147") });

// Configure authenticated HttpClient for API access
// Use the same explicit BaseAddress for consistency
builder.Services.AddScoped<ApiService>();
builder.Services.AddHttpClient("ServerAPI", 
    client => client.BaseAddress = new Uri("https://localhost:7147")) 
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Add Microsoft authentication support
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/User.Read");
    options.ProviderOptions.LoginMode = "redirect";
});

// Add Radzen Blazor services
builder.Services.AddRadzenComponents();

// Add logging services
builder.Services.AddLogging();

await builder.Build().RunAsync();
