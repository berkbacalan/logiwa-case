using Microsoft.Extensions.Logging;

namespace EcomMMS.API.Middleware
{
    public class CachePerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CachePerformanceMiddleware> _logger;

        public CachePerformanceMiddleware(RequestDelegate next, ILogger<CachePerformanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var originalBodyStream = context.Response.Body;

            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                await _next(context);

                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalBodyStream);

                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                if (context.Request.Path.StartsWithSegments("/api/products") || 
                    context.Request.Path.StartsWithSegments("/api/categories"))
                {
                    _logger.LogInformation(
                        "API Performance - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {Duration}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        duration.TotalMilliseconds);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

    public static class CachePerformanceMiddlewareExtensions
    {
        public static IApplicationBuilder UseCachePerformanceMonitoring(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CachePerformanceMiddleware>();
        }
    }
} 