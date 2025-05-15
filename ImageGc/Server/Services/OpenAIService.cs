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
{    private readonly ILogger<OpenAIService> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly string _endpoint;
    private readonly string _key;
    private readonly string _chatCompletionsDeployment;
    private readonly string _imageGenerationDeployment;
    private readonly string _fallbackChatModel;
    
    public OpenAIService(
        IConfiguration configuration,
        ILogger<OpenAIService> logger,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
        
        _endpoint = configuration["OpenAI:Endpoint"] ?? 
            throw new ArgumentNullException("OpenAI:Endpoint is not configured");
        _key = configuration["OpenAI:Key"] ??
            throw new ArgumentNullException("OpenAI:Key is not configured");
        _chatCompletionsDeployment = configuration["OpenAI:ChatCompletionsDeployment"] ?? 
            "gpt-35-turbo";
        _imageGenerationDeployment = configuration["OpenAI:ImageGenerationDeployment"] ?? 
            "dall-e-3";
        _fallbackChatModel = configuration["OpenAI:FallbackChatModel"] ?? 
            "gpt-35-turbo";
            
        _logger.LogInformation("OpenAI Service initialized with chat model: {ChatModel}, image model: {ImageModel}, fallback model: {FallbackModel}",
            _chatCompletionsDeployment, _imageGenerationDeployment, _fallbackChatModel);
    }    /// <summary>
    /// Enhances a basic image description to be more detailed using OpenAI
    /// </summary>
    public async Task<(string EnhancedDescription, int TokensUsed, long ProcessingTimeMs)> EnhanceDescriptionAsync(
        string basicDescription, List<string> tags, int targetLength)
    {
        _logger.LogInformation("Enhancing description with Azure OpenAI. Target length: {TargetLength} words", targetLength);
        var startTime = DateTime.UtcNow;
        var attemptedModels = new List<string>();
        
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
                DeploymentName = _chatCompletionsDeployment,
                Messages =
                {
                    new ChatRequestSystemMessage(@"You are an expert image description enhancer. Your task is to take basic image descriptions and tags and expand them into detailed, vivid descriptions suitable for image generation."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 800,
                Temperature = 0.7f,
                NucleusSamplingFactor = 0.95f
            };
            
            attemptedModels.Add(_chatCompletionsDeployment);
            Response<ChatCompletions> response;
            
            try
            {
                // Try with primary model
                response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "unavailable_model" && _chatCompletionsDeployment != _fallbackChatModel)
            {
                // If primary model unavailable, try fallback
                _logger.LogWarning("Primary model {PrimaryModel} unavailable. Trying fallback model {FallbackModel}", 
                    _chatCompletionsDeployment, _fallbackChatModel);
                
                chatCompletionsOptions.DeploymentName = _fallbackChatModel;
                attemptedModels.Add(_fallbackChatModel);
                
                response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
                
                _logger.LogInformation("Successfully used fallback model {FallbackModel}", _fallbackChatModel);
            }
            
            var enhancedDescription = response.Value.Choices[0].Message.Content.Trim();
            var tokensUsed = response.Value.Usage.TotalTokens;
            
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger.LogInformation("Description enhancement completed in {ProcessingTime}ms, tokens used: {TokensUsed}, model: {Model}", 
                processingTime, tokensUsed, chatCompletionsOptions.DeploymentName);
            
            // Track metrics in Application Insights
            _telemetryClient.TrackMetric("OpenAIEnhancementTime", processingTime);
            _telemetryClient.TrackMetric("OpenAIEnhancementTokens", tokensUsed);
            
            // Track model name as a custom property
            var enhancementTelemetry = new Microsoft.ApplicationInsights.DataContracts.TraceTelemetry("OpenAI Description Enhancement Completed");
            enhancementTelemetry.Properties["Model"] = chatCompletionsOptions.DeploymentName;
            _telemetryClient.TrackTrace(enhancementTelemetry);
            
            return (enhancedDescription, tokensUsed, processingTime);
        }
        catch (Exception ex)
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error enhancing description with Azure OpenAI after {ProcessingTime}ms. Attempted models: {Models}", 
                processingTime, string.Join(", ", attemptedModels));
            
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "AzureOpenAIEnhancement" },
                { "ProcessingTime", processingTime.ToString() },
                { "AttemptedModels", string.Join(", ", attemptedModels) }
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
                DeploymentName = _imageGenerationDeployment,
                Prompt = description,
                Size = ImageSize.Size1024x1024,
                Quality = ImageGenerationQuality.Standard,
                Style = ImageGenerationStyle.Natural
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
            
            _logger.LogInformation("Image generation completed in {ProcessingTime}ms, estimated tokens: {Tokens}, model: {Model}", 
                processingTime, estimatedTokens, _imageGenerationDeployment);
            
            // Track metrics in Application Insights
            _telemetryClient.TrackMetric("DALLEGenerationTime", processingTime);
            _telemetryClient.TrackMetric("DALLEEstimatedTokens", estimatedTokens);
            
            // Track model name as a custom property
            var imageTelemetry = new Microsoft.ApplicationInsights.DataContracts.TraceTelemetry("DALL-E Image Generation Completed");
            imageTelemetry.Properties["Model"] = _imageGenerationDeployment;
            _telemetryClient.TrackTrace(imageTelemetry);
            
            return (imageData, contentType, estimatedTokens, processingTime);
        }
        catch (RequestFailedException ex) when (ex.Status == 500 || ex.ErrorCode == "internal_server_error")
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "DALL-E internal server error after {ProcessingTime}ms - The service may be temporarily unavailable", processingTime);
            
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "AzureOpenAIImageGeneration" },
                { "ProcessingTime", processingTime.ToString() },
                { "Model", _imageGenerationDeployment },
                { "ErrorType", "InternalServerError" }
            });
            
            throw new InvalidOperationException("The image generation service is temporarily unavailable. Please try again later.", ex);
        }
        catch (Exception ex)
        {
            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error generating image with DALL-E after {ProcessingTime}ms", processingTime);
            
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "Service", "AzureOpenAIImageGeneration" },
                { "ProcessingTime", processingTime.ToString() },
                { "Model", _imageGenerationDeployment }
            });
            
            throw; // Rethrow to be handled by the controller
        }
    }
}
