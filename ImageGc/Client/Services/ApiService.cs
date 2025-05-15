using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Json;

namespace Client.Services;

/// <summary>
/// Service for making authenticated HTTP requests to the server API
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly NavigationManager _navigationManager;

    public ApiService(
        HttpClient httpClient,
        ILogger<ApiService> logger,
        NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _logger = logger;
        _navigationManager = navigationManager;
    }

    /// <summary>
    /// Performs an HTTP POST request with authentication, retries, and extended timeouts
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string uri, TRequest request)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            _logger.LogInformation("Making authenticated POST request to {Uri}", uri);
            
            // Configure HttpClient with a longer timeout (3 minutes) for long-running image operations
            _httpClient.Timeout = TimeSpan.FromMinutes(3);
            
            int maxRetries = 2;
            int retryDelayMs = 2000; // Start with 2 second delay
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try 
                {
                    var response = await _httpClient.PostAsJsonAsync(uri, request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Request to {Uri} successful. Status: {Status}", 
                            uri, response.StatusCode);
                        return await response.Content.ReadFromJsonAsync<TResponse>();
                    }
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger.LogWarning("Authentication failed for request to {Uri}. Status: {Status}", 
                            uri, response.StatusCode);
                        
                        // Redirect to the login page if unauthorized
                        _navigationManager.NavigateTo("authentication/login", true);
                        return null;
                    }
                    
                    // Handle server errors or timeouts with retry logic
                    if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                    {
                        if (attempt < maxRetries)
                        {
                            _logger.LogWarning("Request to {Uri} failed with status {Status}. Retrying in {Delay}ms. Attempt {Attempt}/{MaxRetries}", 
                                uri, response.StatusCode, retryDelayMs, attempt + 1, maxRetries);
                            
                            await Task.Delay(retryDelayMs);
                            retryDelayMs *= 2; // Exponential backoff
                            continue; // Retry the request
                        }
                    }
                    
                    // If we get here, it's a non-retryable error or we've exhausted retries
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Request to {Uri} failed. Status: {Status}, Error: {Error}", 
                        uri, response.StatusCode, errorContent);
                    
                    throw new HttpRequestException($"API request failed: {response.StatusCode}, {errorContent}");
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    // Handle timeout exceptions specifically
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning(ex, "Request to {Uri} timed out. Retrying in {Delay}ms. Attempt {Attempt}/{MaxRetries}", 
                            uri, retryDelayMs, attempt + 1, maxRetries);
                        
                        await Task.Delay(retryDelayMs);
                        retryDelayMs *= 2; // Exponential backoff
                        continue; // Retry the request
                    }
                    
                    _logger.LogError(ex, "Request to {Uri} timed out after {MaxRetries} retries", uri, maxRetries);
                    throw new TimeoutException($"Request to {uri} timed out after {maxRetries} retries", ex);
                }
                catch (Exception ex) when (attempt < maxRetries) 
                {
                    // Handle other exceptions with retry
                    _logger.LogWarning(ex, "Error making request to {Uri}. Retrying in {Delay}ms. Attempt {Attempt}/{MaxRetries}", 
                        uri, retryDelayMs, attempt + 1, maxRetries);
                    
                    await Task.Delay(retryDelayMs);
                    retryDelayMs *= 2; // Exponential backoff
                    continue; // Retry the request
                }
            }
            
            // This code should be unreachable if the retry loop works correctly
            throw new InvalidOperationException("Unexpected failure in retry loop");
        }
        catch (AccessTokenNotAvailableException ex)
        {
            _logger.LogWarning(ex, "Access token not available, redirecting to authentication");
            ex.Redirect();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making request to {Uri}", uri);
            throw;
        }
    }

    /// <summary>
    /// Performs an HTTP GET request with authentication
    /// </summary>
    public async Task<TResponse?> GetAsync<TResponse>(string uri)
        where TResponse : class
    {
        try
        {
            _logger.LogInformation("Making authenticated GET request to {Uri}", uri);
            
            // Configure HttpClient with a reasonable timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            int maxRetries = 1; // Fewer retries for GET requests
            int retryDelayMs = 1000; // Start with 1 second delay
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try 
                {
                    var response = await _httpClient.GetAsync(uri);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Request to {Uri} successful. Status: {Status}", 
                            uri, response.StatusCode);
                        return await response.Content.ReadFromJsonAsync<TResponse>();
                    }
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger.LogWarning("Authentication failed for request to {Uri}. Status: {Status}", 
                            uri, response.StatusCode);
                        
                        // Redirect to the login page if unauthorized
                        _navigationManager.NavigateTo("authentication/login", true);
                        return null;
                    }
                    
                    // Handle server errors or timeouts with retry logic
                    if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                    {
                        if (attempt < maxRetries)
                        {
                            _logger.LogWarning("Request to {Uri} failed with status {Status}. Retrying in {Delay}ms. Attempt {Attempt}/{MaxRetries}", 
                                uri, response.StatusCode, retryDelayMs, attempt + 1, maxRetries);
                            
                            await Task.Delay(retryDelayMs);
                            retryDelayMs *= 2; // Exponential backoff
                            continue; // Retry the request
                        }
                    }
                    
                    // If we get here, it's a non-retryable error or we've exhausted retries
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Request to {Uri} failed. Status: {Status}, Error: {Error}", 
                        uri, response.StatusCode, errorContent);
                    
                    throw new HttpRequestException($"API request failed: {response.StatusCode}, {errorContent}");
                }
                catch (Exception ex) when (attempt < maxRetries) 
                {
                    // Handle exceptions with retry
                    _logger.LogWarning(ex, "Error making request to {Uri}. Retrying in {Delay}ms. Attempt {Attempt}/{MaxRetries}", 
                        uri, retryDelayMs, attempt + 1, maxRetries);
                    
                    await Task.Delay(retryDelayMs);
                    retryDelayMs *= 2; // Exponential backoff
                    continue; // Retry the request
                }
            }
            
            // This code should be unreachable if the retry loop works correctly
            throw new InvalidOperationException("Unexpected failure in retry loop");
        }
        catch (AccessTokenNotAvailableException ex)
        {
            _logger.LogWarning(ex, "Access token not available, redirecting to authentication");
            ex.Redirect();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making request to {Uri}", uri);
            throw;
        }
    }
}