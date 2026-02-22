using Serilog.Context;

namespace ProxySharp.Middleware;

public class CorrelationIdMiddleware : IMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            }
            return Task.CompletedTask;
        });

        // Push correlation ID to Serilog log context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // If the incoming request has a correlation ID header, use it; otherwise, generate a new one
        // This allows us to maintain the traceability across distributed systems, and at the consumer/producer levels.
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}

// Using this you expose the middleware by creating an extension method using IApplicationBuilder:
// This is more idiomatic to ASP.NET Core and allows you to easily add the middleware to the pipeline in Program.cs with app.UseCorrelationMiddleware();
// In my opinion, this is a cleaner approach than using app.UseMiddleware<CorrelationIdMiddleware>() in Program.cs,
// as it abstracts away the implementation details and provides a more intuitive API for adding the middleware to the pipeline.
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}