namespace ArimartEcommerceAPI.API.Middleware
{
    public class CustomCorsMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomCorsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var origin = context.Request.Headers["Origin"].ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Check if request is from Capacitor (mobile app)
            bool isCapacitor = string.IsNullOrEmpty(origin) || origin == "capacitor://localhost" ||
                               userAgent.Contains("Capacitor", StringComparison.OrdinalIgnoreCase);

            if (isCapacitor)
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                context.Response.Headers["Access-Control-Allow-Methods"] = "*";
                context.Response.Headers["Access-Control-Allow-Headers"] = "*";
            }

            // Handle preflight OPTIONS request
            if (context.Request.Method == HttpMethods.Options)
            {
                context.Response.StatusCode = 200;
                await context.Response.CompleteAsync();
                return;
            }

            await _next(context);
        }
    }


}
