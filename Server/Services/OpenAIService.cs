using Azure.AI.OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Server.Services;

/// <summary>
/// Interface for OpenAI service operations
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
/// Implementation of OpenAI service using OpenAI SDK
/// </summary>
public class OpenAIService : IOpenAIService
{    private readonly ILogger<OpenAIService> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _chatModel;
    private readonly string _imageModel;
    private readonly string _fallbackChatModel;
    
    public OpenAIService(
        IConfiguration configuration,
        ILogger<OpenAIService> logger,
        TelemetryClient telemetryClient)
    {        _logger = logger;
        _telemetryClient = telemetryClient;
        
        _endpoint = configuration["OpenAI:Endpoint"] ?? 
            throw new ArgumentNullException("OpenAI:Endpoint is not configured");
        _apiKey = configuration["OpenAI:ApiKey"] ?? 
            throw new ArgumentNullException("OpenAI:ApiKey is not configured");
        _chatModel = configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";
        _imageModel = configuration["OpenAI:ImageModel"] ?? "dall-e-3";
        _fallbackChatModel = configuration["OpenAI:FallbackChatModel"] ?? "gpt-4o-mini";
            
        _logger.LogInformation("OpenAI Service initialized with chat model: {ChatModel}, image model: {ImageModel}, fallback model: {FallbackModel}",
            _chatModel, _imageModel, _fallbackChatModel);
    }

    /// <summary>
    /// Enhances a basic image description to be more detailed using OpenAI
    /// </summary>
    public async Task<(string EnhancedDescription, int TokensUsed, long ProcessingTimeMs)> EnhanceDescriptionAsync(
        string basicDescription, List<string> tags, int targetLength)
    {
        // Validate inputs
        if (basicDescription == null)
            throw new ArgumentNullException(nameof(basicDescription));
        if (tags == null)
            throw new ArgumentNullException(nameof(tags));
        if (targetLength <= 0)
            throw new ArgumentException("Target length must be greater than 0", nameof(targetLength));

        _logger.LogInformation("Enhancing description with OpenAI. Target length: {TargetLength} words", targetLength);
        var startTime = DateTime.UtcNow;
        var attemptedModels = new List<string>();
          try
        {            // Create the Azure OpenAI client
            var client = new AzureOpenAIClient(new Uri(_endpoint), new Azure.AzureKeyCredential(_apiKey));
            var chatClient = client.GetChatClient(_chatModel);
            
            // Build the prompt
            var prompt = $@"I have an image with the following basic description:
""{basicDescription}""

The image has been tagged with these elements: {string.Join(", ", tags)}

Please enhance this description to be more detailed and comprehensive, focusing on the visual elements present.
The enhanced description should be approximately {targetLength} words long and suitable for image generation with DALL-E.
The description should be factual based on the information provided and not add fictional elements.

Enhanced description:";
            
            // Configure the chat completion options
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"You are an expert image description enhancer. Your task is to take basic image descriptions and tags and expand them into detailed, vivid descriptions suitable for image generation."),
                new UserChatMessage(prompt)
            };
            
            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 800,
                Temperature = 0.7f,
                TopP = 0.95f
            };
            
            attemptedModels.Add(_chatModel);
            ChatCompletion response;
            
            try
            {
                // Try with primary model
                response = await chatClient.CompleteChatAsync(messages, chatOptions);
            }
            catch (Exception ex) when (ex.Message.Contains("unavailable") && _chatModel != _fallbackChatModel)
            {
                // If primary model unavailable, try fallback
                _logger.LogWarning("Primary model {PrimaryModel} unavailable. Trying fallback model {FallbackModel}", 
                    _chatModel, _fallbackChatModel);
                
                var fallbackChatClient = client.GetChatClient(_fallbackChatModel);
                attemptedModels.Add(_fallbackChatModel);
                
                response = await fallbackChatClient.CompleteChatAsync(messages, chatOptions);
                
                _logger.LogInformation("Successfully used fallback model {FallbackModel}", _fallbackChatModel);
            }
            
            var enhancedDescription = response.Content[0].Text.Trim();
            var tokensUsed = response.Usage.TotalTokenCount;
            
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogInformation("Description enhancement completed in {ProcessingTime}ms, tokens used: {TokensUsed}", 
                processingTime, tokensUsed);
            
            // Track metrics in Application Insights
            _telemetryClient.TrackMetric("OpenAIEnhancementTime", processingTime);
            _telemetryClient.TrackMetric("OpenAIEnhancementTokens", tokensUsed);
            
            // Track model name as a custom property
            var enhancementTelemetry = new Microsoft.ApplicationInsights.DataContracts.TraceTelemetry("OpenAI Description Enhancement Completed");
            enhancementTelemetry.Properties["Model"] = _chatModel;
            _telemetryClient.TrackTrace(enhancementTelemetry);
            
            return (enhancedDescription, tokensUsed, processingTime);
        }
        catch (Exception ex)
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error enhancing description with OpenAI after {ProcessingTime}ms. Attempted models: {Models}", 
                processingTime, string.Join(", ", attemptedModels));
            
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "OpenAIEnhancement" },
                { "ProcessingTime", processingTime.ToString() },
                { "AttemptedModels", string.Join(", ", attemptedModels) }
            });
            
            throw; // Rethrow to be handled by the controller
        }
    }

    /// <summary>
    /// Generates a new image based on a description using DALL-E
    /// </summary>
    public async Task<(byte[] ImageData, string ContentType, int TokensUsed, long ProcessingTimeMs)> GenerateImageAsync(
        string description)
    {
        // Validate inputs
        if (description == null)
            throw new ArgumentNullException(nameof(description));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty or whitespace", nameof(description));

        _logger.LogInformation("Generating image with DALL-E based on description");
        var startTime = DateTime.UtcNow;
          try
        {            // Create the Azure OpenAI client
            var client = new AzureOpenAIClient(new Uri(_endpoint), new Azure.AzureKeyCredential(_apiKey));
            var imageClient = client.GetImageClient(_imageModel);
            
            // Configure the image generation options
            var imageGenerationOptions = new ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1024xH1024,
                Quality = GeneratedImageQuality.Standard,
                Style = GeneratedImageStyle.Natural,
                ResponseFormat = GeneratedImageFormat.Bytes
            };
            
            // Generate the image
            var response = await imageClient.GenerateImageAsync(description, imageGenerationOptions);
            var generatedImage = response.Value;
            
            // Get binary data from the image
            byte[] imageData = generatedImage.ImageBytes.ToArray();
            var contentType = "image/png"; // DALL-E generates PNG images
            
            // Estimate token usage based on prompt length
            var estimatedTokens = description.Length / 4; // Very rough estimate
            
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogInformation("Image generation completed in {ProcessingTime}ms, estimated tokens: {Tokens}, model: {Model}", 
                processingTime, estimatedTokens, _imageModel);
            
            // Track metrics in Application Insights
            _telemetryClient.TrackMetric("DALLEGenerationTime", processingTime);
            _telemetryClient.TrackMetric("DALLEEstimatedTokens", estimatedTokens);
            
            // Track model name as a custom property
            var imageTelemetry = new Microsoft.ApplicationInsights.DataContracts.TraceTelemetry("DALL-E Image Generation Completed");
            imageTelemetry.Properties["Model"] = _imageModel;
            _telemetryClient.TrackTrace(imageTelemetry);
            
            return (imageData, contentType, estimatedTokens, processingTime);
        }
        catch (Exception ex)
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error generating image with DALL-E after {ProcessingTime}ms", processingTime);
            
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "OpenAIImageGeneration" },
                { "ProcessingTime", processingTime.ToString() },
                { "Model", _imageModel }
            });
            
            throw new InvalidOperationException("The image generation service is temporarily unavailable. Please try again later.", ex);
        }
    }
}
