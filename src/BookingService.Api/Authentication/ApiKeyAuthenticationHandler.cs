using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BookingService.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key authentication is not configured on the server."));
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = headerValue.ToString();

        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key is empty."));
        }

        // Use constant-time comparison to prevent timing attacks
        var expectedBytes = Encoding.UTF8.GetBytes(Options.ApiKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyUser"),
            new Claim(ClaimTypes.AuthenticationMethod, SchemeName)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        return Response.WriteAsync(
            """{"type":"https://httpstatuses.com/401","title":"Unauthorized","detail":"A valid API key is required. Provide it via the X-Api-Key header.","status":401}""");
    }
}

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
