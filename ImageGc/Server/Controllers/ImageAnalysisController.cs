using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using ImageGc.Shared.Models;
using Server.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageAnalysisController : ControllerBase
{
    private readonly ILogger<ImageAnalysisController> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IComputerVisionService _computerVisionService;
    private readonly IOpenAIService _openAIService;
    
    public ImageAnalysisController(
        ILogger<ImageAnalysisController> logger, 
        TelemetryClient telemetryClient,
        IComputerVisionService computerVisionService,
        IOpenAIService openAIService)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        _computerVisionService = computerVisionService;
        _openAIService = openAIService;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<ImageAnalysisResult>> AnalyzeImage([FromBody] ImageAnalysisRequest request)
    {
        try
        {
            // Get the authenticated user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
            
            _logger.LogInformation("Image analysis request received from user {UserId} ({UserName}). File: {FileName}, Description Length: {Length} words", 
                userId, userName, request.FileName, request.DescriptionLength);
            
            // Track request with Application Insights
            var stopwatch = Stopwatch.StartNew();
            _telemetryClient.TrackEvent("ImageAnalysisRequested", new Dictionary<string, string>
            {
                { "UserId", userId },
                { "UserName", userName },
                { "FileType", request.ContentType },
                { "DescriptionLength", request.DescriptionLength.ToString() }
            });
            
            // Prepare the result object
            var result = new ImageAnalysisResult();
            
            // Convert base64 image data to bytes
            byte[] imageBytes;
            try
            {
                // Extract the actual base64 data if it contains the data URL prefix
                string base64Data = request.ImageData;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Split(',')[1];
                }
                
                imageBytes = Convert.FromBase64String(base64Data);
                _logger.LogInformation("Successfully converted image data: {Size} bytes", imageBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode base64 image data");
                return BadRequest(new { error = "Invalid image data format" });
            }
            
            // Step 1: Analyze image with Computer Vision
            _logger.LogInformation("Step 1: Analyzing image with Computer Vision");
            string basicDescription;
            List<string> tags;
            double confidenceScore;
            try
            {
                var (description, imageTags, confidence, processingTime) = 
                    await _computerVisionService.AnalyzeImageAsync(imageBytes);
                
                basicDescription = description;
                tags = imageTags;
                confidenceScore = confidence;
                result.Metrics.ImageAnalysisTimeMs = processingTime;
                
                _logger.LogInformation("Computer Vision analysis completed. Description: {Length} characters, Tags: {TagCount}", 
                    basicDescription.Length, tags.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Computer Vision analysis");
                result.Metrics.ErrorInfo = $"Computer Vision analysis failed: {ex.Message}";
                return StatusCode(500, result);
            }
            
            // Step 2: Enhance description with OpenAI
            _logger.LogInformation("Step 2: Enhancing description with OpenAI");
            string enhancedDescription;
            try
            {
                var (description, tokensUsed, processingTime) = 
                    await _openAIService.EnhanceDescriptionAsync(basicDescription, tags, request.DescriptionLength);
                
                enhancedDescription = description;
                result.Metrics.DescriptionGenerationTimeMs = processingTime;
                result.Metrics.DescriptionTokensUsed = tokensUsed;
                
                _logger.LogInformation("Description enhancement completed. Length: {Length} characters, Tokens: {Tokens}", 
                    enhancedDescription.Length, tokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during description enhancement");
                // Fall back to the basic description
                enhancedDescription = basicDescription;
                result.Metrics.ErrorInfo = $"Description enhancement failed: {ex.Message}";
            }
            
            // Step 3: Generate new image with DALL-E
            _logger.LogInformation("Step 3: Generating image with DALL-E");
            try
            {
                var (imageData, contentType, tokensUsed, processingTime) = 
                    await _openAIService.GenerateImageAsync(enhancedDescription);
                
                result.RegeneratedImageData = Convert.ToBase64String(imageData);
                result.RegeneratedImageContentType = contentType;
                result.Metrics.ImageRegenerationTimeMs = processingTime;
                result.Metrics.RegenerationTokensUsed = tokensUsed;
                
                _logger.LogInformation("Image generation completed. Size: {Size} bytes, Tokens: {Tokens}", 
                    imageData.Length, tokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image generation");
                result.Metrics.ErrorInfo = $"Image generation failed: {ex.Message}";
                // We'll return what we have so far
            }
            
            // Complete the result
            result.Description = enhancedDescription;
            result.Tags = tags;
            result.ConfidenceScore = confidenceScore;
            
            stopwatch.Stop();
            var totalTime = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation("Image analysis completed in {TotalTime}ms for user {UserName}", totalTime, userName);
            
            // Track metrics
            _telemetryClient.TrackMetric("TotalProcessingTime", totalTime);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing image analysis request");
            _telemetryClient.TrackException(ex);
            
            return StatusCode(500, new ImageAnalysisResult
            {
                Metrics = new ProcessingMetrics
                {
                    ErrorInfo = $"Error: {ex.Message}"
                }
            });
        }
    }
}