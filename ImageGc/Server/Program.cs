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
else if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:InstrumentationKey"]))
{
    telemetryConfig.InstrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(
        telemetryConfig,
        TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();

// Explicitly configure Kestrel endpoints
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Loopback, 5257); // HTTP
    serverOptions.Listen(IPAddress.Loopback, 7147, listenOptions => // HTTPS
    {
        listenOptions.UseHttps(); // Use default dev cert
    });
});

// Add services to the container.
builder.Services.AddApplicationInsightsTelemetry();

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

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
builder.Services.AddScoped<IComputerVisionService, ComputerVisionService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseCors("AllowAll");
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles(); // Serve Blazor WebAssembly static files
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
// This ensures any routes not matched by controllers or Razor pages are handled by the Blazor app
app.MapFallbackToFile("index.html");

// Create log.txt if it doesn't exist
if (!File.Exists("log.txt"))
{
    File.Create("log.txt").Dispose();
    Log.Information("Log file created");
}

try
{
    Log.Information("Starting application - Server hosting Blazor WebAssembly Client");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
