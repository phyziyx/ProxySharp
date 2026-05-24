using ProxySharp.Exceptions;

namespace ProxySharp.Services;

public class TokenStore
{
    // Semaphore to ensure thread-safe access to the token
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _semaphoreTimeout = TimeSpan.FromSeconds(30);
    // Data
    private string? _token;
    private DateTime _expiresAt = DateTime.MinValue;
    private DateTime _lastRefreshFailure = DateTime.MinValue;
    // Constants
    private static readonly TimeSpan FAILURE_PERIOD = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan GRACE_PERIOD = TimeSpan.FromSeconds(60);
    // Logger
    private readonly ILogger<TokenStore> _logger;

    public TokenStore(ILogger<TokenStore> logger, IConfiguration config)
    {
        _logger = logger;
        _semaphoreTimeout = TimeSpan.FromSeconds(Convert.ToDouble(config["AuthTimeout"]));
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
            lockTaken = await _lock.WaitAsync(_semaphoreTimeout, ct);

            // Recheck inside lock (double-check locking)
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt - GRACE_PERIOD)
            {
                return _token!;
            }

            // Caching problem: Thundering herd - if req 1, req 2, ... req N fails then subsequent requests will keep failing
            // so we need to put a cooldown for a bit...
            if (DateTime.UtcNow - _lastRefreshFailure < FAILURE_PERIOD)
            {
                throw new AuthUnavailableException();
            }

            // Refresh the token using the injected refresh function
            var (newToken, newExpiry) = await refreshFunc();
            _token = newToken;
            _expiresAt = newExpiry;
            _lastRefreshFailure = DateTime.MinValue;

            return _token!;
        }
        catch
        {
            _lastRefreshFailure = DateTime.Now;
            // Re-throw the exception!
            throw;
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
            lockTaken = await _lock.WaitAsync(_semaphoreTimeout, ct);

            _token = token;
            _expiresAt = expires;

            _lastRefreshFailure = DateTime.MinValue;
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
