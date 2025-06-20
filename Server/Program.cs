using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Serilog;
using Serilog.Events;
using Server.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var telemetryConfig = new TelemetryConfiguration();
if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    telemetryConfig.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
}
// Removed obsolete InstrumentationKey fallback

Log.Logger = new LoggerConfiguration() // Uncommented Serilog setup
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext() // Uncommented
    .WriteTo.Console() // Uncommented
    .WriteTo.File("log.txt", 
        fileSizeLimitBytes: 10_000_000,  // 10MB file size limit
        rollOnFileSizeLimit: true,       // Create a new file when size limit is reached
        retainedFileCountLimit: 1,       // Only keep the current log file
        shared: true)                    // Allow multiple processes to write to the file
    .WriteTo.ApplicationInsights( // Uncommented
        telemetryConfig,
        TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog(); // Uncommented Serilog host integration

// Add services to the container.
// Kestrel endpoints will be configured automatically based on environment variables (e.g., PORT in Azure App Service)
builder.Services.AddApplicationInsightsTelemetry();

// Add authentication
var hasValidAzureAdConfig = !string.IsNullOrEmpty(builder.Configuration["AzureAd:ClientId"]) && 
    builder.Configuration["AzureAd:ClientId"] != "11111111-1111-1111-11111111111111111";

if (hasValidAzureAdConfig)
{
    Log.Information("Configuring Azure AD authentication");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}
else
{
    Log.Warning("Azure AD configuration not found or using placeholder values. Authentication disabled.");
}

// Make authentication optional by default
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // Remove the default requirement for authentication
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add CORS policy for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register Azure services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IComputerVisionService, ComputerVisionService>(); // Uncommented
builder.Services.AddScoped<IOpenAIService, OpenAIService>(); // Re-enabled

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Enable CORS for all environments (required for Azure hosting)
app.UseCors("AllowAll");

app.UseHttpsRedirection(); // Enabled for proper HTTPS handling
app.UseBlazorFrameworkFiles(); // Serve Blazor WebAssembly static files
app.UseStaticFiles();

app.UseRouting();

// Only use authentication if it was configured
if (hasValidAzureAdConfig)
{
    app.UseAuthentication();
}
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
// This ensures any routes not matched by controllers or Razor pages are handled by the Blazor app
app.MapFallbackToFile("index.html");

// Clean up any existing log files except the main log.txt
try
{
    var logFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "log*.txt")
                            .Where(f => !f.EndsWith("log.txt"));
    foreach (var file in logFiles)
    {
        File.Delete(file);
        Log.Information($"Deleted old log file: {Path.GetFileName(file)}");
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "Error cleaning up old log files");
}

// Create log.txt if it doesn't exist
if (!File.Exists("log.txt"))
{
    File.Create("log.txt").Dispose();
    // Log.Information("Log file created"); // Keep commented
}

try // Uncommented final try/catch/finally
{
    Log.Information("Starting application - Server hosting Blazor WebAssembly Client"); // Uncommented Serilog log
    // Console.WriteLine("Starting application - Server hosting Blazor WebAssembly Client (Console Log)"); // Remove console log
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly"); // Uncommented Serilog log
    // Console.WriteLine($"FATAL ERROR: {ex}"); // Remove console log
}
finally
{
    Log.CloseAndFlush(); // Uncommented Serilog log
}

// Make Program class public for testing
public partial class Program { }
