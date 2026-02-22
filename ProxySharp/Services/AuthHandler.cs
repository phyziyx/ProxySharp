namespace ProxySharp.Services;

public class AuthHandler(TokenStore tokenStore, ITokenService tokenService, ILogger<AuthHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the current (or new) token
        var token = await tokenStore.GetOrRefreshTokenAsync(tokenService.RequestNewTokenAsync);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        // If unauthorized, refresh and retry once
        if (System.Net.HttpStatusCode.Unauthorized == response.StatusCode)
        {
            logger.LogWarning("Received 401, refreshing token and retrying...");

            var (newToken, expires) = await tokenService.RequestNewTokenAsync();
            await tokenStore.ForceUpdateAsync(newToken, expires);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);

            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}

