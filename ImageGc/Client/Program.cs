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
// Use relative URL when hosted by the server
var baseAddress = builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

// Configure authenticated HttpClient for API access
builder.Services.AddScoped<ApiService>();
builder.Services.AddHttpClient("ServerAPI", 
    client => client.BaseAddress = new Uri(baseAddress)) 
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
builder.Services.AddLogging(logging => 
{
    // Set log level for authentication components to Debug for more details
    logging.SetMinimumLevel(LogLevel.Debug); 
    logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly.Authentication", LogLevel.Debug);
    // Enable PII logging for detailed auth errors (DEBUG ONLY)
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true; 
});

await builder.Build().RunAsync();
