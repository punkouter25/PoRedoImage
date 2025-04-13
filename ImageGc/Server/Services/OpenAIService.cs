using Azure;
using Azure.AI.OpenAI;
using Microsoft.ApplicationInsights;

namespace Server.Services;

/// <summary>
/// Interface for Azure OpenAI service operations
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// Enhances a basic image description to a more detailed one
    /// </summary>
    /// <param name="basicDescription">The basic description from Computer Vision</param>
    /// <param name="tags">Related tags from Computer Vision</param>
    /// <param name="targetLength">Target word count for the enhanced description</param>
    /// <returns>Enhanced description and token usage</returns>
    Task<(string EnhancedDescription, int TokensUsed, long ProcessingTimeMs)> EnhanceDescriptionAsync(
        string basicDescription, List<string> tags, int targetLength);
    
    /// <summary>
    /// Generates a new image based on a description using DALL-E
    /// </summary>
    /// <param name="description">Description to base the image on</param>
    /// <returns>Generated image data, content type, and token usage</returns>
    Task<(byte[] ImageData, string ContentType, int TokensUsed, long ProcessingTimeMs)> GenerateImageAsync(
        string description);
}

/// <summary>
/// Implementation of OpenAI service using Azure OpenAI SDK
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly ILogger<OpenAIService> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly string _endpoint;
    private readonly string _key;
    private readonly string _textDeploymentName; // Renamed for clarity
    private readonly string _imageDeploymentName; // Added for DALL-E
    
    public OpenAIService(
        IConfiguration configuration,
        ILogger<OpenAIService> logger,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        
        _endpoint = configuration["AzureOpenAI:Endpoint"] ?? 
            throw new ArgumentNullException("OpenAI Endpoint is not configured");
        _key = configuration["AzureOpenAI:Key"] ??
            throw new ArgumentNullException("OpenAI Key is not configured");
        _textDeploymentName = configuration["AzureOpenAI:DeploymentName"] ?? // Keep original key for text model
            throw new ArgumentNullException("OpenAI DeploymentName (for text) is not configured");
        _imageDeploymentName = configuration["AzureOpenAI:ImageDeploymentName"] ?? // New key for image model
            throw new ArgumentNullException("OpenAI ImageDeploymentName (for DALL-E) is not configured");
    }

    /// <summary>
    /// Enhances a basic image description to be more detailed using OpenAI
    /// </summary>
    public async Task<(string EnhancedDescription, int TokensUsed, long ProcessingTimeMs)> EnhanceDescriptionAsync(
        string basicDescription, List<string> tags, int targetLength)
    {
        _logger.LogInformation("Enhancing description with Azure OpenAI. Target length: {TargetLength} words", targetLength);
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Create the OpenAI client
            var credential = new AzureKeyCredential(_key);
            var client = new OpenAIClient(new Uri(_endpoint), credential);
            
            // Build the prompt
            var prompt = $@"I have an image with the following basic description:
""{basicDescription}""

The image has been tagged with these elements: {string.Join(", ", tags)}

Please enhance this description to be more detailed and comprehensive, focusing on the visual elements present.
The enhanced description should be approximately {targetLength} words long and suitable for image generation with DALL-E.
The description should be factual based on the information provided and not add fictional elements.

Enhanced description:";
            
            // Configure the chat completion options
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _textDeploymentName, // Use text deployment
                Messages =
                {
                    new ChatRequestSystemMessage(@"You are an expert image description enhancer. Your task is to take basic image descriptions and tags and expand them into detailed, vivid descriptions suitable for image generation."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 800, // Adjust based on your requirements
                Temperature = 0.7f,
                NucleusSamplingFactor = 0.95f
            };
            
            // Get the response
            var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
            var enhancedDescription = response.Value.Choices[0].Message.Content.Trim();
            var tokensUsed = response.Value.Usage.TotalTokens;
            
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogInformation("Description enhancement completed in {ProcessingTime}ms, tokens used: {TokensUsed}", 
                processingTime, tokensUsed);
            
            // Track metrics in Application Insights
            _telemetryClient.TrackMetric("OpenAIEnhancementTime", processingTime);
            _telemetryClient.TrackMetric("OpenAIEnhancementTokens", tokensUsed);
            
            return (enhancedDescription, tokensUsed, processingTime);
        }
        catch (Exception ex)
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error enhancing description with Azure OpenAI after {ProcessingTime}ms", processingTime);
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "AzureOpenAIEnhancement" },
                { "ProcessingTime", processingTime.ToString() }
            });
            
            throw; // Rethrow to be handled by the controller
        }
    }
    
    /// <summary>
    /// Generates a new image based on a description using DALL-E through Azure OpenAI
    /// </summary>
    public async Task<(byte[] ImageData, string ContentType, int TokensUsed, long ProcessingTimeMs)> GenerateImageAsync(
        string description)
    {
        _logger.LogInformation("Generating image with DALL-E based on description");
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Create the OpenAI client
            var credential = new AzureKeyCredential(_key);
            var client = new OpenAIClient(new Uri(_endpoint), credential);
            
            // Configure the image generation options
            var imageGenerationOptions = new ImageGenerationOptions
            {
                DeploymentName = _imageDeploymentName, // Use image deployment
                Prompt = description,
                Size = ImageSize.Size1024x1024,
                Quality = ImageGenerationQuality.Standard,
                Style = ImageGenerationStyle.Natural,
                // Removed ResponseFormat property as it's no longer available
            };
            
            // Generate the image
            var response = await client.GetImageGenerationsAsync(imageGenerationOptions);
            var generatedImage = response.Value.Data[0];
            
            // Get binary data from the image URL or data
            byte[] imageData;
            if (!string.IsNullOrEmpty(generatedImage.Base64Data))
            {
                // If base64 data is available
                imageData = Convert.FromBase64String(generatedImage.Base64Data);
            }
            else if (generatedImage.Url != null)
            {
                // If URL is provided, download the image
                using var httpClient = new HttpClient();
                imageData = await httpClient.GetByteArrayAsync(generatedImage.Url);
            }
            else
            {
                throw new InvalidOperationException("No image data or URL returned from DALL-E");
            }
            
            var contentType = "image/png"; // DALL-E generates PNG images
            
            // Estimate token usage based on prompt length
            var estimatedTokens = description.Length / 4; // Very rough estimate
            
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogInformation("Image generation completed in {ProcessingTime}ms, estimated tokens: {Tokens}", 
                processingTime, estimatedTokens);
            
            // Track metrics in Application Insights
            _telemetryClient.TrackMetric("DALLEGenerationTime", processingTime);
            _telemetryClient.TrackMetric("DALLEEstimatedTokens", estimatedTokens);
            
            return (imageData, contentType, estimatedTokens, processingTime);
        }
        catch (Exception ex)
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error generating image with DALL-E after {ProcessingTime}ms", processingTime);
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "AzureOpenAIImageGeneration" },
                { "ProcessingTime", processingTime.ToString() }
            });
            
            throw; // Rethrow to be handled by the controller
        }
    }
}
