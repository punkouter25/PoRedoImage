using Microsoft.AspNetCore.Mvc;
using ImageGc.Shared.Models;
using Server.Services;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageAnalysisController : ControllerBase
{    private readonly ILogger<ImageAnalysisController> _logger;
    private readonly IComputerVisionService _computerVisionService;
    private readonly IOpenAIService _openAIService;
    
    public ImageAnalysisController(
        ILogger<ImageAnalysisController> logger, 
        IComputerVisionService computerVisionService,
        IOpenAIService openAIService)
    {
        _logger = logger;
        _computerVisionService = computerVisionService;
        _openAIService = openAIService;
    }    [HttpPost("analyze")]
    [AllowAnonymous] // Allow anonymous access for debugging
    public async Task<ActionResult<ImageAnalysisResult>> AnalyzeImage([FromBody] ImageAnalysisRequest request)
    {
        try
        {
            // Get the authenticated user (allow anonymous for now)
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var userName = User?.FindFirst(ClaimTypes.Name)?.Value ?? "anonymous";
            
            _logger.LogInformation("Image analysis request received from user {UserId} ({UserName}). File: {FileName}, Description Length: {Length} words", 
                userId, userName, request.FileName, request.DescriptionLength);
            
            // Prepare the result object
            var result = new ImageAnalysisResult();
            
            // Convert base64 image data to bytes
            var stopwatch = Stopwatch.StartNew();
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

            // Server-side validation for file type and size
            const int MaxFileSize = 20 * 1024 * 1024; // 20MB
            if (imageBytes.Length > MaxFileSize)
            {
                _logger.LogWarning("Received image exceeds maximum size limit. Size: {Size} bytes", imageBytes.Length);
                return BadRequest(new { error = $"File size exceeds the maximum allowed ({MaxFileSize / 1024 / 1024}MB)." });
            }

            if (request.ContentType != "image/jpeg" && request.ContentType != "image/png")
            {
                _logger.LogWarning("Received image with unsupported content type: {ContentType}", request.ContentType);
                return BadRequest(new { error = "Only JPG and PNG files are supported." });
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
            }            // Step 2: Enhance description with OpenAI
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
            }            // Step 3: Generate new image with DALL-E
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
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing image analysis request");
            
            return StatusCode(500, new ImageAnalysisResult
            {
                Metrics = new ProcessingMetrics
                {
                    ErrorInfo = $"Error: {ex.Message}"
                }
            });
        }
    }

    [HttpGet("test")]
    public ActionResult<object> TestConnection()
    {
        _logger.LogInformation("Test endpoint called");
        return Ok(new { 
            message = "ImageAnalysis API is working", 
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    [HttpGet("debug")]
    [AllowAnonymous]
    public ActionResult<object> DebugInfo()
    {
        _logger.LogInformation("Debug endpoint called");
        
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
        var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
        var claims = User?.Claims?.Select(c => new { c.Type, c.Value })?.ToArray() ?? Array.Empty<object>();
        
        return Ok(new { 
            message = "Debug info for ImageAnalysis API", 
            timestamp = DateTime.UtcNow,
            environment = environment,
            isAuthenticated = isAuthenticated,
            userClaims = claims,
            requestHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            baseUrl = $"{Request.Scheme}://{Request.Host}",
            path = Request.Path
        });
    }
}
