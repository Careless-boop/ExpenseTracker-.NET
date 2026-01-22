using ExpenseTracker.Application.Common.Exceptions;
using System.Text.Json;

namespace ExpenseTracker.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, response) = exception switch
            {
                ValidationException validationEx => (
                    StatusCodes.Status400BadRequest,
                    new { error = "Validation failed", details = validationEx.Errors }),

                NotFoundException notFoundEx => (
                    StatusCodes.Status404NotFound,
                    new { error = notFoundEx.Message } as object),

                ForbiddenException forbiddenEx => (
                    StatusCodes.Status403Forbidden,
                    new { error = forbiddenEx.Message } as object),

                UnauthorizedAccessException => (
                    StatusCodes.Status401Unauthorized,
                    new { error = "Unauthorized" } as object),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    new { error = "An unexpected error occurred" } as object)
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred");
            }
            else
            {
                _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
