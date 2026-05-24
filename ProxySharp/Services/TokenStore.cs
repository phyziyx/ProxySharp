namespace ProxySharp.Services;

public class TokenStore
{
    // Semaphore to ensure thread-safe access to the token
    private readonly SemaphoreSlim _lock = new(1, 1);
    // Data
    private string? _token;
    private DateTime _expiresAt = DateTime.MinValue;
    // Constants
    private static readonly TimeSpan GRACE_PERIOD = TimeSpan.FromSeconds(60);
    // Logger
    private readonly ILogger<TokenStore> _logger;

    public TokenStore(ILogger<TokenStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns a valid token, refreshing it if missing or about to expire.
    /// </summary>
    public async Task<string> GetOrRefreshTokenAsync(Func<Task<(string token, DateTime expires)>> refreshFunc, CancellationToken ct)
    {
        bool lockTaken = false;

        // If missing or within the grace window, refresh
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt - GRACE_PERIOD)
        {
            _logger.LogInformation("Token is valid, returning existing token.");
            return _token!;
        }

        // Acquire lock to refresh token, if needed
        _logger.LogInformation("Acquiring lock...");
        try
        {
            // SemaphoreSlim will return a boolean depending whether the lock has been acquired or not.
            // This is useful if the lock has not been acquired.
            lockTaken = await _lock.WaitAsync(ct);

            // Recheck inside lock (double-check locking)
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt - GRACE_PERIOD)
            {
                return _token!;
            }

            // Refresh the token using the injected refresh function
            var (newToken, newExpiry) = await refreshFunc();
            _token = newToken;
            _expiresAt = newExpiry;

            return _token!;
        }
        finally
        {
            // Only release the lock if acquired earlier.
            if (lockTaken)
            {
                _lock.Release();
            }
        }
    }

    public async Task ForceUpdateAsync(string token, DateTime expires, CancellationToken ct)
    {
        bool lockTaken = false;

        try
        {
            lockTaken = await _lock.WaitAsync(ct);

            _token = token;
            _expiresAt = expires;
        }
        finally
        {
            if (lockTaken)
            {
                _lock.Release();
            }
        }
    }

    public bool IsTokenExpired() => DateTime.UtcNow >= _expiresAt;
}
