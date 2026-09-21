using System.Net;
using System.Text.Json;
using CRMWebServices.Helpers;

namespace CRMWebServices.Helpers
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        #region InvokeAsync Used for handling exceptions globally in the application. It catches any unhandled exceptions that occur during the request processing pipeline and returns a standardized error response to the client.
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unhandled exception on {Method} {Path}",context.Request.Method,context.Request.Path);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = new ApiResponse
                {
                    Status = false,
                    Message = "An unexpected error occurred. Please try again later.",
                    Data = null
                };

                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
        #endregion
    }
}
