using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IConfiguration _configuration;
    private readonly IComputerVisionService? _computerVisionService;
    private readonly IOpenAIService? _openAIService;

    public HealthController(
        ILogger<HealthController> logger, 
        TelemetryClient telemetryClient,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        _configuration = configuration;
        
        // Try to resolve services if they're available, but don't fail if they're not
        _computerVisionService = serviceProvider.GetService<IComputerVisionService>();
        _openAIService = serviceProvider.GetService<IOpenAIService>();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult CheckHealth()
    {
        _logger.LogInformation("Executing CheckHealth action."); // Add log here
        try
        {
            // Log authentication status
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var userId = isAuthenticated 
                ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown"
                : "anonymous";
            
            _logger.LogInformation("API health check requested by {UserId}, authenticated: {IsAuthenticated}", 
                userId, isAuthenticated);
            
            _telemetryClient.TrackEvent("HealthCheckRequested", new Dictionary<string, string>
            {
                { "UserId", userId },
                { "IsAuthenticated", isAuthenticated.ToString() }
            });
            
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                user = isAuthenticated ? userId : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }

    [HttpGet("azure")]
    [AllowAnonymous]
    public IActionResult CheckAzureServices() // Removed async Task<>
    {
        _logger.LogInformation("Executing CheckAzureServices action."); // Add log here
        try
        {
            _logger.LogInformation("Azure services health check requested");
            
            var services = new List<object>();
            var overallStatus = "healthy";
            
            // Check Application Insights
            var appInsightsKey = _configuration["ApplicationInsights:InstrumentationKey"];
            bool appInsightsConfigured = !string.IsNullOrEmpty(appInsightsKey) && appInsightsKey != "development-key";
            services.Add(new { 
                name = "Application Insights", 
                status = appInsightsConfigured ? "healthy" : "not configured",
                details = appInsightsConfigured ? "Configuration found" : "Missing or using development key"
            });
            
            // Check Computer Vision API
            var cvEndpoint = _configuration["AzureComputerVision:Endpoint"];
            var cvKey = _configuration["AzureComputerVision:Key"];
            bool cvConfigured = !string.IsNullOrEmpty(cvEndpoint) && !string.IsNullOrEmpty(cvKey) && 
                                !cvEndpoint.Contains("your-vision-service") && cvKey != "development-key";
            services.Add(new { 
                name = "Computer Vision API", 
                status = cvConfigured ? "configured" : "not configured",
                details = cvConfigured 
                    ? $"Endpoint configured: {cvEndpoint.Substring(0, Math.Min(cvEndpoint.Length, 30))}..." 
                    : "Endpoint or key not properly configured"
            });
            
            // Check OpenAI API
            var openaiEndpoint = _configuration["AzureOpenAI:Endpoint"];
            var openaiKey = _configuration["AzureOpenAI:Key"];
            var openaiDeployment = _configuration["AzureOpenAI:DeploymentName"];
            bool openaiConfigured = !string.IsNullOrEmpty(openaiEndpoint) && !string.IsNullOrEmpty(openaiKey) && 
                                   !string.IsNullOrEmpty(openaiDeployment) && 
                                   !openaiEndpoint.Contains("your-openai-service") && openaiKey != "development-key";
            services.Add(new { 
                name = "Azure OpenAI", 
                status = openaiConfigured ? "configured" : "not configured",
                details = openaiConfigured 
                    ? $"Endpoint configured: {openaiEndpoint.Substring(0, Math.Min(openaiEndpoint.Length, 30))}..." 
                    : "Endpoint, key, or deployment name not properly configured"
            });

            // Check Authentication Configuration
            var authClientId = _configuration["AzureAd:ClientId"];
            var authTenantId = _configuration["AzureAd:TenantId"];
            bool authConfigured = !string.IsNullOrEmpty(authClientId) && !string.IsNullOrEmpty(authTenantId) && 
                                 authClientId != "33333333-3333-3333-33333333333333333";
            services.Add(new { 
                name = "Authentication", 
                status = authConfigured ? "configured" : "not configured",
                details = authConfigured 
                    ? "Client ID and tenant configured" 
                    : "Using placeholder values for client ID or tenant"
            });
            
            // If any service is not configured, set overall status to warning
            if (!appInsightsConfigured || !cvConfigured || !openaiConfigured || !authConfigured)
            {
                overallStatus = "warning";
            }
            
            return Ok(new { 
                status = overallStatus, 
                services = services,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure services health check failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }
}
