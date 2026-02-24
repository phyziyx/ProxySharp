using ProxySharp.Infrastructure.Http;
using ProxySharp.Middleware;
using ProxySharp.Services;
using Serilog;
using System.Threading.RateLimiting;

namespace ProxySharp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/proxysharp-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add configuration from appsettings.json to our configuration class with validation
        builder.Services.AddOptions<Models.Configuration>()
            .Bind(builder.Configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Add services to the container
        builder.Services.AddSingleton<TokenStore>();
        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddScoped<AuthHandler>();

        // According to:
        // (1) http://milanjovanovic.tech/blog/the-right-way-to-use-httpclient-in-dotnet
        // (2) https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting?source=recommendations#typed-client-has-the-wrong-httpclient-injected
        //
        // Typed clients also use named clients under the hood with a caveat (it also registers
        // a Transient service using the TClient or TClient,TImplementation provided).
        //
        // The difference is that we do not need to specify the name when creating the client
        // Which is more convenient, especially when we have multiple clients with different configs.

        // Configure a named HttpClient for authentication
        builder.Services.AddHttpClient("AuthClient")
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!);
                client.Timeout = TimeSpan.FromSeconds(Convert.ToDouble(builder.Configuration["AuthTimeout"]));
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                HttpClientProxyFactory.CreateProxyHandler(builder.Configuration)
            );

        // Configure a typed HttpClient for our service
        builder.Services.AddHttpClient<TestServiceClient>()
            .ConfigurePrimaryHttpMessageHandler(() =>
                HttpClientProxyFactory.CreateProxyHandler(builder.Configuration)
            )
            .AddHttpMessageHandler<AuthHandler>();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure rate limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Fixed window rate limiter: 60 requests per minute per IP
            options.AddPolicy("fixed", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Sliding window rate limiter for stricter control
            options.AddPolicy("sliding", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Global rate limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    }));
        });

        var app = builder.Build();

        // Add correlation ID middleware to ensure all logs have a correlation ID for better traceability
        app.UseCorrelationMiddleware();

        // [added for testing] Add HTTP logging middleware to log incoming requests and outgoing responses
        app.UseHttpLogging();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Add rate limiting to all requests - we do not want bad actors to overload our API or cause performance issues
        // This also helps us detect if consumers of our API are behaving incorrectly by exceeding appropriate limits
        app.UseRateLimiter();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
