using System.Net;
using System.Text.Json;

namespace ProxySharp.Services;

public class ClientResult<T>
{
    public T? Data { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string? RawBody { get; set; }
}

public class TestServiceClient
{
    private readonly HttpClient _httpClient;
    private JsonSerializerOptions jsonOptions;

    public TestServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        string? baseUrl = configuration["ApiUrl"];
        if (null == baseUrl)
        {
            throw new NullReferenceException("ApiUrl is not configured in appsettings.json");
        }

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(Double.Parse(configuration["RequestTimeout"]));
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private string NormaliseEndpoint(string endpoint)
    {
        if (endpoint.StartsWith("/"))
        {
            endpoint = endpoint.Substring(1);
        }
        return endpoint;
    }

    private Uri BuildUri(string endpoint)
    {
        var normalised = NormaliseEndpoint(endpoint);
        return new Uri(_httpClient.BaseAddress, normalised);
    }

    public async Task<ClientResult<T>> GetAsync<T>(string endpoint)
    {
        var uri = BuildUri(endpoint);
        var response = await _httpClient.GetAsync(uri);

        var rawContent = await response.Content.ReadAsStringAsync();

        T? body = default;
        if (response.IsSuccessStatusCode)
        {
            if (!string.IsNullOrWhiteSpace(rawContent))
            {
                body = await response.Content.ReadFromJsonAsync<T>();
            }
        }

        return new ClientResult<T>
        {
            Data = body,
            StatusCode = response.StatusCode,
            RawBody = rawContent
        };
    }

    public async Task<ClientResult<TOut?>> PostAsync<TOut, TIn>(string endpoint, TIn data)
    {
        var uri = BuildUri(endpoint);
        var response = await _httpClient.PostAsJsonAsync(uri, data);

        var rawContent = await response.Content.ReadAsStringAsync();

        TOut? body = default;
        if (response.IsSuccessStatusCode)
        {
            if (!string.IsNullOrWhiteSpace(rawContent))
            {
                body = JsonSerializer.Deserialize<TOut>(rawContent, jsonOptions);
            }
        }

        return new ClientResult<TOut?>
        {
            Data = body,
            StatusCode = response.StatusCode,
            RawBody = rawContent
        };
    }
}
