using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string ApiKeyQueryParam = "apikey"; // URL parameter name

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        // Skip API key check for OPTIONS requests (CORS preflight)
        if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip API key check for Swagger UI
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/api-docs"))
        {
            await _next(context);
            return;
        }

        string extractedApiKey = null;

        // Try to get API key from header first
        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerApiKey))
        {
            extractedApiKey = headerApiKey.ToString();
        }
        // If not in header, try URL query parameter
        else if (context.Request.Query.TryGetValue(ApiKeyQueryParam, out var queryApiKey))
        {
            extractedApiKey = queryApiKey.ToString();
        }

        // If no API key found in either location
        if (string.IsNullOrEmpty(extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key is missing. Provide it as 'X-Api-Key' header or 'apikey' query parameter.");
            return;
        }

        var apiKey = config.GetValue<string>("ApiKey");
        if (!apiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        await _next(context);
    }
}
