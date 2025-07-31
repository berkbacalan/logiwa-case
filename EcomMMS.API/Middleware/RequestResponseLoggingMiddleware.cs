using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace EcomMMS.API.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            context.Response.Headers.Add("X-Request-ID", requestId);
            
            await LogRequest(context, requestId);
            
            var originalBodyStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await LogException(context, requestId, ex, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);
                
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalBodyStream);
            }
        }

        private async Task LogRequest(HttpContext context, string requestId)
        {
            var request = context.Request;
            var requestBody = string.Empty;

            if (request.Method is "POST" or "PUT" or "PATCH")
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            _logger.LogInformation(
                "HTTP Request - {Method} {Path} - RequestId: {RequestId}, UserAgent: {UserAgent}, IP: {IP}, ContentType: {ContentType}, ContentLength: {ContentLength}, Body: {RequestBody}",
                request.Method,
                request.Path,
                requestId,
                request.Headers.UserAgent.ToString(),
                context.Connection.RemoteIpAddress?.ToString(),
                request.ContentType,
                request.ContentLength,
                requestBody);
        }

        private async Task LogResponse(HttpContext context, string requestId, long durationMs)
        {
            var response = context.Response;
            var responseBody = string.Empty;

            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
            responseBody = await reader.ReadToEndAsync();
            context.Response.Body.Position = 0;

            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(
                logLevel,
                "HTTP Response - {Method} {Path} - RequestId: {RequestId}, StatusCode: {StatusCode}, Duration: {Duration}ms, ResponseSize: {ResponseSize}bytes, ContentType: {ContentType}, Body: {ResponseBody}",
                context.Request.Method,
                context.Request.Path,
                requestId,
                response.StatusCode,
                durationMs,
                Encoding.UTF8.GetByteCount(responseBody),
                response.ContentType,
                responseBody);
        }

        private async Task LogException(HttpContext context, string requestId, Exception ex, long durationMs)
        {
            _logger.LogError(
                ex,
                "HTTP Exception - {Method} {Path} - RequestId: {RequestId}, Duration: {Duration}ms, Exception: {ExceptionMessage}",
                context.Request.Method,
                context.Request.Path,
                requestId,
                durationMs,
                ex.Message);
        }
    }
} 