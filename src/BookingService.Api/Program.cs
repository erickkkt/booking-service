using BookingService.Api.Authentication;
using BookingService.Api.Middleware;
using BookingService.Application.DependencyInjection;
using BookingService.Infrastructure.DependencyInjection;
using BookingService.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Structured logging with Serilog (Step 7.1)
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "BookingService")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
        {
            Name = ApiKeyAuthenticationHandler.HeaderName,
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Description = "API key authentication via the X-Api-Key header."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = ApiKeyAuthenticationHandler.SchemeName
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Authentication: API key via X-Api-Key header
    builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.SchemeName,
            options => options.ApiKey = builder.Configuration["Authentication:ApiKey"] ?? string.Empty);
    builder.Services.AddAuthorization();

    // Application layer: CQRS (MediatR) + FluentValidation (Step 2)
    builder.Services.AddApplication();

    // Infrastructure layer: EF Core, Outbox, Service Bus, Idempotency (Steps 2, 4, 5)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Distributed tracing with OpenTelemetry (Step 7.3)
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("BookingService", serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Security headers (X-Content-Type-Options, X-Frame-Options, CSP, HSTS, etc.)
    app.UseSecurityHeaders();

    app.UseHttpsRedirection();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Idempotency middleware for POST /api/bookings (Step 5)
    app.UseIdempotencyMiddleware();

    // Add request logging for observability (Step 7.1)
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            if (httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey))
                diagnosticContext.Set("IdempotencyKey", idempotencyKey.ToString());
        };
    });

    app.MapControllers();
    app.MapGet("/", () => Results.Redirect("/swagger"));

    app.Run();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

