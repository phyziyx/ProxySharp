namespace ProxySharp.Services;

public class AuthHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthHandler> _logger;

    public AuthHandler(TokenStore tokenStore, ITokenService tokenService, ILogger<AuthHandler> logger)
    {
        _tokenStore = tokenStore;
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the current (or new) token
        var token = await _tokenStore.GetOrRefreshTokenAsync(_tokenService.RequestNewTokenAsync);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        //Console.WriteLine("Request Headers:");
        //foreach (var h in request.Headers)
        //{
        //    Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
        //}

        //if (request.Content != null)
        //{
        //    Console.WriteLine("Content Headers:");
        //    foreach (var h in request.Content.Headers)
        //    {
        //        Console.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
        //    }
        //}

        var response = await base.SendAsync(request, cancellationToken);

        // If unauthorized, refresh and retry once
        if (System.Net.HttpStatusCode.Unauthorized == response.StatusCode)
        {
            _logger.LogWarning("Received 401, refreshing token and retrying...");

            var (newToken, expires) = await _tokenService.RequestNewTokenAsync();
            await _tokenStore.ForceUpdateAsync(newToken, expires);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);

            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}

