using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Application.Common;
using Ordering.Application.DTOs;

namespace Ordering.Infrastructure.Identity;

public class KeycloakService(
    HttpClient httpClient,
    IOptions<KeycloakOptions> options,
    ILogger<KeycloakService> logger,
    ICacheService cacheService) : IKeycloakService
{
    //    private DateTime _tokenExpiration = DateTime.MinValue;

    public async Task<string> GetAdminTokenAsync()
    {
        //if(!string.IsNullOrEmpty(await cacheService.GetAsync("keycloak-token")) && DateTime.UtcNow < _tokenExpiration)

        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", options.Value.ClientId),
            new KeyValuePair<string, string>("client_secret", options.Value.ClientSecret),
            new KeyValuePair<string, string>("grant_type", options.Value.GrantType)
        ]);

        var response = await httpClient.PostAsync(
            $"{options.Value.BaseUrl}/realms/{options.Value.Realm}/protocol/openid-connect/token", content);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to get Keycloak admin token. Status code {StatusCode}", response.StatusCode);
            throw new Exception("Failed to retrieve Keycloak admin token.");
        }

        var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(await response.Content.ReadAsStringAsync());
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new Exception("Invalid token response from Keycloak.");
        }

        //await cacheService.SetAsync("keycloak-token", tokenResponse.AccessToken, TimeSpan.FromMinutes(5));

        return tokenResponse.AccessToken;
    }

    public async Task<KeycloakUser?> GetUserByUserIdAsync(Guid userId)
    {
        var token = await GetAdminTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"{options.Value.BaseUrl}/admin/realms/{options.Value.Realm}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize<KeycloakUser>(await response.Content.ReadAsStringAsync());

        logger.LogError("Failed to fetch user from Keycloak. Status Code {StatusCode}", response.StatusCode);
        return null;
    }
}