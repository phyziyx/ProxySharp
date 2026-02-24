using System.Net;

namespace ProxySharp.Infrastructure.Http;

public static class HttpClientProxyFactory
{
    public static HttpMessageHandler CreateProxyHandler(IConfiguration config)
    {
        var proxyUrl = config["Proxy:Url"];

        // The reason to use SocketsHttpHandler instead of HttpClientHandler is
        // to deal with potential socket exhaustion failures in heavy load apps.

        if (string.IsNullOrEmpty(proxyUrl))
            return new SocketsHttpHandler();

        var proxy = new WebProxy(proxyUrl)
        {
            Credentials = string.IsNullOrEmpty(config["Proxy:Username"])
                ? null
                : new NetworkCredential(
                    config["Proxy:Username"],
                    config["Proxy:Password"])
        };

        return new SocketsHttpHandler
        {
            Proxy = proxy,
            UseProxy = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 50
        };
    }
}