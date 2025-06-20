using Microsoft.Extensions.Configuration;
using System.Net.NetworkInformation;
using System.Net;
using Xunit;

namespace ImageGc.Tests.Connectivity;

/// <summary>
/// Tests for verifying connectivity to external services and dependencies
/// </summary>
public class ConnectivityTests : TestBase
{
    [Fact]
    public async Task InternetConnectivity_ShouldBeAvailable()
    {
        // Arrange
        const string testHost = "www.microsoft.com";
        
        // Act & Assert
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(testHost, 5000);
            
            Assert.True(reply.Status == IPStatus.Success, 
                $"Internet connectivity test failed. Ping to {testHost} returned: {reply.Status}");
        }
        catch (PingException ex)
        {
            Assert.Fail($"Internet connectivity test failed with exception: {ex.Message}");
        }
    }

    // [Fact]
    // public async Task AzureComputerVisionEndpoint_ShouldBeReachable()
    // {
    //     // Arrange
    //     var endpoint = Configuration["ComputerVision:Endpoint"];
    //     Assert.NotNull(endpoint);

    //     // Act & Assert
    //     try
    //     {
    //         using var client = new HttpClient();
    //         client.Timeout = TimeSpan.FromSeconds(10);
            
    //         var response = await client.GetAsync(endpoint);
            
    //         // We expect some response from the endpoint (even if it's an error due to missing auth)
    //         // The important thing is that we can reach the endpoint
    //         Assert.True(response.StatusCode != HttpStatusCode.ServiceUnavailable &&
    //                    response.StatusCode != HttpStatusCode.RequestTimeout,
    //                    $"Computer Vision endpoint {endpoint} appears to be unreachable. Status: {response.StatusCode}");
    //     }
    //     catch (HttpRequestException ex)
    //     {
    //         Assert.Fail($"Failed to reach Computer Vision endpoint {endpoint}: {ex.Message}");
    //     }
    //     catch (TaskCanceledException)
    //     {
    //         Assert.Fail($"Timeout reached when trying to connect to Computer Vision endpoint {endpoint}");
    //     }
    // }

    // [Fact]
    // public async Task AzureOpenAIEndpoint_ShouldBeReachable()
    // {
    //     // Arrange
    //     var endpoint = Configuration["OpenAI:Endpoint"];
    //     Assert.NotNull(endpoint);

    //     // Act & Assert
    //     try
    //     {
    //         using var client = new HttpClient();
    //         client.Timeout = TimeSpan.FromSeconds(10);
            
    //         var response = await client.GetAsync(endpoint);
            
    //         // We expect some response from the endpoint (even if it's an error due to missing auth)
    //         // The important thing is that we can reach the endpoint
    //         Assert.True(response.StatusCode != HttpStatusCode.ServiceUnavailable &&
    //                    response.StatusCode != HttpStatusCode.RequestTimeout,
    //                    $"OpenAI endpoint {endpoint} appears to be unreachable. Status: {response.StatusCode}");
    //     }
    //     catch (HttpRequestException ex)
    //     {
    //         Assert.Fail($"Failed to reach OpenAI endpoint {endpoint}: {ex.Message}");
    //     }
    //     catch (TaskCanceledException)
    //     {
    //         Assert.Fail($"Timeout reached when trying to connect to OpenAI endpoint {endpoint}");
    //     }
    // }

    // [Theory]
    // [InlineData("ComputerVision:Endpoint")]
    // [InlineData("OpenAI:Endpoint")]
    // public void ConfiguredEndpoints_ShouldBeValidUrls(string configKey)
    // {
    //     // Arrange
    //     var endpoint = Configuration[configKey];

    //     // Act & Assert
    //     Assert.NotNull(endpoint);
    //     Assert.NotEmpty(endpoint);
    //     Assert.True(Uri.IsWellFormedUriString(endpoint, UriKind.Absolute),
    //         $"Configuration key {configKey} does not contain a valid URL: {endpoint}");
        
    //     var uri = new Uri(endpoint);
    //     Assert.Equal("https", uri.Scheme, $"Endpoint {configKey} should use HTTPS");
    // }

    // [Fact]
    // public async Task MultipleEndpoints_ConcurrentConnectivity_ShouldAllSucceed()
    // {
    //     // Arrange
    //     var endpoints = new string?[]
    //     {
    //         Configuration["ComputerVision:Endpoint"],
    //         Configuration["OpenAI:Endpoint"]
    //     };

    //     // Act
    //     var validEndpoints = endpoints.Where(endpoint => !string.IsNullOrEmpty(endpoint)).ToArray();
    //     var tasks = validEndpoints.Select(async endpoint =>
    //     {
    //         using var client = new HttpClient();
    //         client.Timeout = TimeSpan.FromSeconds(10);
            
    //         try
    //         {
    //             var response = await client.GetAsync(endpoint!);
    //             return new { Endpoint = endpoint, Success = true, StatusCode = response.StatusCode };
    //         }
    //         catch (Exception)
    //         {
    //             return new { Endpoint = endpoint, Success = false, StatusCode = HttpStatusCode.InternalServerError };
    //         }
    //     });

    //     var results = await Task.WhenAll(tasks);

    //     // Assert
    //     foreach (var result in results)
    //     {
    //         Assert.True(result.Success || 
    //                    result.StatusCode == HttpStatusCode.Unauthorized ||
    //                    result.StatusCode == HttpStatusCode.Forbidden,
    //                    $"Failed to reach endpoint {result.Endpoint}. Status: {result.StatusCode}");
    //     }
    // }

    // [Fact]
    // public async Task DnsResolution_ForAzureEndpoints_ShouldWork()
    // {
    //     // Arrange
    //     var endpoints = new[]
    //     {
    //         "eastus.api.cognitive.microsoft.com",
    //         "api.openai.com"
    //     };

    //     // Act & Assert
    //     foreach (var hostname in endpoints)
    //     {
    //         try
    //         {
    //             var addresses = await Dns.GetHostAddressesAsync(hostname);
    //             Assert.NotEmpty(addresses);
    //             Assert.All(addresses, addr => Assert.True(
    //                 addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
    //                 addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6));
    //         }
    //         catch (Exception ex)
    //         {
    //             Assert.Fail($"DNS resolution failed for {hostname}: {ex.Message}");
    //         }
    //     }
    // }

    // [Fact]
    // public void ApplicationInsights_Configuration_ShouldBeValid()
    // {
    //     // Arrange
    //     var instrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];

    //     // Act & Assert
    //     Assert.NotNull(instrumentationKey);
    //     Assert.NotEmpty(instrumentationKey);
        
    //     // Instrumentation key should be a GUID format or connection string
    //     if (Guid.TryParse(instrumentationKey, out _))
    //     {
    //         // Valid GUID format
    //         Assert.True(true);
    //     }
    //     else
    //     {
    //         // Should be a connection string format or placeholder
    //         var isValidConnectionString = instrumentationKey.IndexOf("InstrumentationKey=", StringComparison.OrdinalIgnoreCase) >= 0;
    //         var isPlaceholder = instrumentationKey.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0;
    //         Assert.True(isValidConnectionString || isPlaceholder, 
    //             "Instrumentation key should be a GUID, connection string, or placeholder");
    //     }
    // }

    // [Fact]
    // public void HttpClient_DefaultSettings_ShouldSupportLargeUploads()
    // {
    //     // Arrange
    //     using var client = new HttpClient();
        
    //     // Act & Assert
    //     // Test that we can handle large image uploads (up to 20MB as per requirements)
    //     Assert.True(client.MaxResponseContentBufferSize >= 20 * 1024 * 1024, 
    //         "HttpClient should support responses up to 20MB");
        
    //     // Test with a reasonable timeout
    //     client.Timeout = TimeSpan.FromMinutes(2);
    //     Assert.True(client.Timeout == TimeSpan.FromMinutes(2), "HttpClient timeout should be 2 minutes");
    // }
}
