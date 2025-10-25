using System.Diagnostics;

namespace RestaurantApi.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var request = context.Request;
        var path = request.Path;
        var method = request.Method;

        _logger.LogInformation("➡️ Incoming request: {Method} {Path}", method, path);

        await _next(context);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation("⬅️ Completed {Method} {Path} in {Elapsed} ms with status {StatusCode}",
            method, path, elapsedMs, context.Response.StatusCode);
    }
}
