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
    /// Performs an HTTP POST request with authentication
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string uri, TRequest request)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            _logger.LogInformation("Making authenticated POST request to {Uri}", uri);
            
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
            
            // Handle other error cases
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Request to {Uri} failed. Status: {Status}, Error: {Error}", 
                uri, response.StatusCode, errorContent);
            
            throw new HttpRequestException($"API request failed: {response.StatusCode}, {errorContent}");
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
            
            // Handle other error cases
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Request to {Uri} failed. Status: {Status}, Error: {Error}", 
                uri, response.StatusCode, errorContent);
            
            throw new HttpRequestException($"API request failed: {response.StatusCode}, {errorContent}");
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