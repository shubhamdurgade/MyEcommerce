using System.Net;
using System.Text.Json;
using AuthServer.Common.Results;
namespace AuthServer.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex) 
            {
                _logger.LogWarning(ex, "Handled AppException Path={Path}, Mehtod={Method}, TraceId={TraceId}, StatusCode={StatusCode}, Message={Message}",context.Request.Path, context.Request.Method,context.TraceIdentifier,ex.StatusCode, ex.Message);
                await WriteErrorAsync(context, ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled Exception Path={Path}, Mehtod={Method}, TraceId={TraceId}", context.Request.Path, context.Request.Method, context.TraceIdentifier);
                await WriteErrorAsync(context, (int)HttpStatusCode.InternalServerError, "Something went wrong. Please try again");
            }

        }

        private async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();

            context.Response.StatusCode = statusCode;

            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(ApiResponse<string>.Fail(message));

            await context.Response.WriteAsync(payload);
        }
    }
}
