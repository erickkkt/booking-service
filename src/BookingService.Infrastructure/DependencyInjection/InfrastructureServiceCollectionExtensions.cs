using Azure.Messaging.ServiceBus;
using BookingService.Application.Abstractions;
using BookingService.Infrastructure.Messaging;
using BookingService.Infrastructure.Outbox;
using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToLowerInvariant() ?? "sqlserver";
        var sqlServerConnectionString = configuration.GetConnectionString("SqlServer");
        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<BookingDbContext>(options =>
        {
            if (provider == "postgres")
            {
                options.UseNpgsql(postgresConnectionString ?? "Host=localhost;Port=5432;Database=booking_service;Username=postgres;Password=postgres");
            }
            else
            {
                options.UseSqlServer(sqlServerConnectionString ?? "Server=localhost,1433;Database=BookingService;User Id=sa;Password=Your_password123;TrustServerCertificate=True");
            }
        });

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IOutboxWriter, EfCoreOutboxWriter>();
        services.AddScoped<IIdempotencyStore, EfCoreIdempotencyStore>();

        var serviceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        var topicName = configuration["ServiceBus:TopicName"] ?? "booking-events";
        var paymentSubscriptionName = configuration["ServiceBus:PaymentSubscriptionName"] ?? "booking-service-payment";

        if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            services.AddSingleton<IOutboxMessageSender, NoOpOutboxMessageSender>();
            // Register PaymentCompletedConsumer in no-op mode (null client)
            services.AddHostedService(sp =>
                new PaymentCompletedConsumer(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PaymentCompletedConsumer>>(),
                    serviceBusClient: null,
                    topicName,
                    paymentSubscriptionName));
        }
        else
        {
            services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
            services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>().CreateSender(topicName));
            services.AddSingleton<IOutboxMessageSender, ServiceBusOutboxMessageSender>();
            services.AddHostedService(sp =>
                new PaymentCompletedConsumer(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PaymentCompletedConsumer>>(),
                    sp.GetRequiredService<ServiceBusClient>(),
                    topicName,
                    paymentSubscriptionName));
        }

        services.AddHostedService<OutboxDispatcher>();

        return services;
    }
}

