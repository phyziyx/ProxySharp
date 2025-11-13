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

    /// <summary>
    /// Returns a valid token, refreshing it if missing or about to expire.
    /// </summary>
    public async Task<string> GetOrRefreshTokenAsync(Func<Task<(string token, DateTime expires)>> refreshFunc)
    {
        // If missing or within the grace window, refresh
        if (string.IsNullOrEmpty(_token) || DateTime.UtcNow >= _expiresAt - GRACE_PERIOD)
        {
            await _lock.WaitAsync();
            try
            {
                // Recheck inside lock (double-check locking)
                if (string.IsNullOrEmpty(_token) || DateTime.UtcNow >= _expiresAt - GRACE_PERIOD)
                {
                    var (newToken, newExpiry) = await refreshFunc();
                    _token = newToken;
                    _expiresAt = newExpiry;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        return _token!;
    }

    public async Task ForceUpdateAsync(string token, DateTime expires)
    {
        await _lock.WaitAsync();
        try
        {
            _token = token;
            _expiresAt = expires;
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool IsTokenExpired() => DateTime.UtcNow >= _expiresAt;
}
