using System.Text.Json.Serialization;

namespace ProxySharp.Services;

public interface ITokenService
{
    Task<(string token, DateTime expires)> RequestNewTokenAsync();
}

public class TokenRequest
{
    [JsonPropertyName("Username")]
    public string Username { get; set; }

    [JsonPropertyName("Password")]
    public string Password { get; set; }
}

public class TokenResponse
{
    [JsonPropertyName("Token")]
    public string? Token { get; set; }

    [JsonPropertyName("ExpiresIn")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("resCode")]
    public int? ResCode { get; set; }
}

public class TokenService(IHttpClientFactory factory, ILogger<TokenService> logger, IConfiguration configuration) : ITokenService
{
    public async Task<(string token, DateTime expires)> RequestNewTokenAsync()
    {
        logger.LogInformation("Requesting new access token...");
        
        var client = factory.CreateClient("AuthClient");

        var username = configuration.GetValue<string>("API_USERNAME");
        var password = configuration.GetValue<string>("API_PASSWORD");

        var response = await client.PostAsJsonAsync("Users/authenticate", new TokenRequest
        {
            Username = username,
            Password = password
        });

        // Print out the response
        Console.WriteLine("Content: " + await response.Content.ReadAsStringAsync());

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (null == result)
        {
            return (string.Empty, DateTime.MinValue);
        }

        int expiresIn = result.ExpiresIn ?? 0;
        string token = result.Token ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token) || expiresIn <= 0)
        {
            return (string.Empty, DateTime.MinValue);
        }

        return (token, DateTime.UtcNow.AddSeconds(expiresIn));
    }
}
