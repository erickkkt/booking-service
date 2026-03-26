using System.Security.Cryptography;
using System.Text;
using BookingService.Application.Abstractions;

namespace BookingService.Api.Middleware;

public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore idempotencyStore)
    {
        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            await _next(context);
            return;
        }

        var key = values.ToString();

        // Step 5: compute request hash to detect payload changes for the same idempotency key
        context.Request.EnableBuffering();
        var requestHash = await ComputeRequestHashAsync(context.Request, context.RequestAborted);
        context.Request.Body.Position = 0;

        var existing = await idempotencyStore.GetAsync(key, context.RequestAborted);
        if (existing is not null)
        {
            // If a different payload is sent with the same key, reject the request (409 Conflict)
            if (existing.RequestHash != requestHash)
            {
                _logger.LogWarning(
                    "Idempotency-Key {IdempotencyKey} reused with a different payload (hash mismatch). Returning 409.",
                    key);
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    """{"type":"https://httpstatuses.com/409","title":"Conflict","detail":"The Idempotency-Key was already used with a different request payload.","status":409}""",
                    context.RequestAborted);
                return;
            }

            _logger.LogInformation(
                "Returning cached idempotent response for key {IdempotencyKey}",
                key);

            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody, context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        responseBody.Position = 0;
        var responseText = await new StreamReader(responseBody).ReadToEndAsync(context.RequestAborted);

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            await idempotencyStore.SaveAsync(
                new IdempotencyRecord(key, requestHash, context.Response.StatusCode, responseText, DateTimeOffset.UtcNow),
                context.RequestAborted);

            _logger.LogInformation("Stored idempotent response for key {IdempotencyKey}", key);
        }

        responseBody.Position = 0;
        await responseBody.CopyToAsync(originalBody, context.RequestAborted);
        context.Response.Body = originalBody;
    }

    private static async Task<string> ComputeRequestHashAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, cancellationToken);
        var bytes = SHA256.HashData(ms.ToArray());
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotencyMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<IdempotencyMiddleware>();
}

