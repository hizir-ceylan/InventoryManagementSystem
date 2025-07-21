using Inventory.Api.Services;
using System.Text;

namespace Inventory.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICentralizedLoggingService loggingService)
        {
            var originalResponseBodyStream = context.Response.Body;

            try
            {
                // Log request details
                await LogRequestAsync(context, loggingService);

                // Create a new memory stream for the response body
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // Execute the next middleware
                await _next(context);

                // Log response details
                await LogResponseAsync(context, responseBodyStream, loggingService);

                // Copy response back to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request logging middleware");
                await loggingService.LogErrorAsync("RequestLogging", "Error in request logging middleware", ex);
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBodyStream;
            }
        }

        private async Task LogRequestAsync(HttpContext context, ICentralizedLoggingService loggingService)
        {
            try
            {
                var request = context.Request;
                var requestBody = string.Empty;

                // Read request body for POST/PUT requests
                if (request.Method == HttpMethod.Post.Method || request.Method == HttpMethod.Put.Method)
                {
                    request.EnableBuffering();
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    request.Body.Position = 0; // Reset stream position
                }

                var logMessage = $"API Request: {request.Method} {request.Path}{request.QueryString} " +
                               $"from {GetClientIpAddress(context)} " +
                               $"UserAgent: {request.Headers["User-Agent"]}";

                if (!string.IsNullOrEmpty(requestBody))
                {
                    logMessage += $" Body: {requestBody}";
                }

                _logger.LogInformation(logMessage);
                await loggingService.LogInfoAsync("API", logMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request");
            }
        }

        private async Task LogResponseAsync(HttpContext context, MemoryStream responseBodyStream, ICentralizedLoggingService loggingService)
        {
            try
            {
                var response = context.Response;
                var responseBody = string.Empty;

                if (responseBodyStream.Length > 0)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
                    responseBody = await reader.ReadToEndAsync();
                }

                var logMessage = $"API Response: {response.StatusCode} for {context.Request.Method} {context.Request.Path} " +
                               $"ContentLength: {responseBodyStream.Length}";

                // Only log response body for error responses or if it's small
                if (response.StatusCode >= 400 || responseBodyStream.Length < 1000)
                {
                    logMessage += $" Body: {responseBody}";
                }

                _logger.LogInformation(logMessage);
                await loggingService.LogInfoAsync("API", logMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging response");
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? "Unknown";
        }
    }
}