using MoonCore.Services;
using Moonlight.Core.Configuration;

namespace Moonlight.Core.Http.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ConfigService<CoreConfiguration> _configService;

        public ApiKeyMiddleware(RequestDelegate next, ConfigService<CoreConfiguration> configService)
        {
            _next = next;
            _configService = configService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get the configuration
            var config = _configService.Get();

            // Get the requested path
            var path = context.Request.Path.Value;

            // Check if the requested path is in the list of authenticated endpoints
            if (config?.AuthenticatedEndpoints != null && config.AuthenticatedEndpoints.Contains(path))
            {
                // Read the API key from the request header
                if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("API key not provided.");
                    return;
                }

                var apiKey = apiKeyHeader.FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("API key not provided.");
                    return;
                }

                // Validate the API key
                if (config?.ApiKeys == null || !config.ApiKeys.Contains(apiKey))
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Invalid API key.");
                    return;
                }
            }

            // If the API key is valid or the endpoint is not in the list, call the next middleware in the pipeline
            await _next(context);
        }
    }
}
