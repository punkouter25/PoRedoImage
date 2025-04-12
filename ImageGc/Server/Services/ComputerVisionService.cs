using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.ApplicationInsights;

namespace Server.Services;

/// <summary>
/// Interface for the Azure Computer Vision service
/// </summary>
public interface IComputerVisionService
{
    /// <summary>
    /// Analyzes an image and generates a description
    /// </summary>
    /// <param name="imageData">The binary image data to analyze</param>
    /// <returns>Analysis result with description and tags</returns>
    Task<(string Description, List<string> Tags, double ConfidenceScore, long ProcessingTimeMs)> AnalyzeImageAsync(
        byte[] imageData);
}

/// <summary>
/// Implementation of Computer Vision service using Azure AI Vision
/// </summary>
public class ComputerVisionService : IComputerVisionService
{
    private readonly ILogger<ComputerVisionService> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly string _endpoint;
    private readonly string _key;
    
    public ComputerVisionService(
        IConfiguration configuration,
        ILogger<ComputerVisionService> logger,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        
        _endpoint = configuration["AzureComputerVision:Endpoint"] ?? 
            throw new ArgumentNullException("Computer Vision Endpoint is not configured");
        _key = configuration["AzureComputerVision:Key"] ?? 
            throw new ArgumentNullException("Computer Vision Key is not configured");
    }
    
    /// <summary>
    /// Analyzes an image and generates a description using Azure Computer Vision
    /// </summary>
    public async Task<(string Description, List<string> Tags, double ConfidenceScore, long ProcessingTimeMs)> AnalyzeImageAsync(
        byte[] imageData)
    {
        _logger.LogInformation("Starting image analysis with Azure Computer Vision");
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Create the client and set the analysis options
            var credential = new AzureKeyCredential(_key);
            var client = new ImageAnalysisClient(new Uri(_endpoint), credential);
            
            // Update: specify visual features instead of analysis options
            // The API has changed and now uses VisualFeatures enum instead of ImageAnalysisOptions.Features
            var visualFeatures = VisualFeatures.Caption | VisualFeatures.Tags;
            
            // Analyze the image
            var response = await client.AnalyzeAsync(
                BinaryData.FromBytes(imageData), 
                visualFeatures,
                new ImageAnalysisOptions { Language = "en", GenderNeutralCaption = true });
            
            // Extract description and tags
            var description = response.Value.Caption?.Text ?? "No description available";
            var confidenceScore = response.Value.Caption?.Confidence ?? 0;
            
            var tags = new List<string>();
            if (response.Value.Tags != null)
            {
                // Access the Tags collection through its Values property
                foreach (var tag in response.Value.Tags.Values)
                {
                    tags.Add(tag.Name);
                }
            }
            
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogInformation("Image analysis completed in {ProcessingTime}ms, confidence: {Confidence}", 
                processingTime, confidenceScore);
            
            // Track metrics in Application Insights
            _telemetryClient.TrackMetric("ComputerVisionProcessingTime", processingTime);
            _telemetryClient.TrackMetric("ComputerVisionConfidence", confidenceScore);
            
            return (description, tags, confidenceScore, processingTime);
        }
        catch (Exception ex)
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error analyzing image with Azure Computer Vision after {ProcessingTime}ms", processingTime);
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "AzureComputerVision" },
                { "ProcessingTime", processingTime.ToString() }
            });
            
            throw; // Rethrow to be handled by the controller
        }
    }
}