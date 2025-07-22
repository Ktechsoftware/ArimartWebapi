namespace ArimartEcommerceAPI.API.Middleware
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

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}",
                    context.Request.Path, context.Request.Method);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An internal server error occurred.",
                Details = exception.Message, // Remove in production
                StackTrace = exception.StackTrace // Remove in production
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
