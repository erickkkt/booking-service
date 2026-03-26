namespace BookingService.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME-type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking
            headers["X-Frame-Options"] = "DENY";

            // Content Security Policy – restrict resource origins
            headers["Content-Security-Policy"] = "default-src 'self'";

            // Enforce HTTPS with HSTS (1 year, include subdomains)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            // Disable legacy XSS filter (modern approach is CSP)
            headers["X-XSS-Protection"] = "0";

            // Control referrer information sent with requests
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict browser features
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Prevent Adobe cross-domain requests
            headers["X-Permitted-Cross-Domain-Policies"] = "none";

            // Prevent caching of API responses that may contain sensitive data
            headers["Cache-Control"] = "no-store";
            headers["Pragma"] = "no-cache";

            // Remove server identification header
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
