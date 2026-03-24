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
        var existing = await idempotencyStore.GetAsync(key, context.RequestAborted);
        if (existing is not null)
        {
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
            await idempotencyStore.SaveAsync(new IdempotencyRecord(key, context.Response.StatusCode, responseText, DateTimeOffset.UtcNow), context.RequestAborted);
            _logger.LogInformation("Stored idempotent response for key {IdempotencyKey}", key);
        }

        responseBody.Position = 0;
        await responseBody.CopyToAsync(originalBody, context.RequestAborted);
        context.Response.Body = originalBody;
    }
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotencyMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<IdempotencyMiddleware>();
}
