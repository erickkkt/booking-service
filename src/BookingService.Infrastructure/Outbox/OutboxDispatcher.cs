using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BookingService.Infrastructure.Persistence;

namespace BookingService.Infrastructure.Outbox;

public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                var sender = scope.ServiceProvider.GetRequiredService<IOutboxMessageSender>();

                var messages = await dbContext.OutboxMessages
                    .Where(x => x.ProcessedAtUtc == null)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await sender.SendAsync(message.Type, message.Payload, stoppingToken);
                        message.ProcessedAtUtc = DateTimeOffset.UtcNow;
                        message.Error = null;
                    }
                    catch (Exception ex)
                    {
                        message.Error = ex.Message;
                        _logger.LogError(ex, "Failed to dispatch outbox message {MessageId}", message.Id);
                    }
                }

                if (messages.Count > 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public interface IOutboxMessageSender
{
    Task SendAsync(string messageType, string payload, CancellationToken cancellationToken = default);
}

public sealed class NoOpOutboxMessageSender : IOutboxMessageSender
{
    private readonly ILogger<NoOpOutboxMessageSender> _logger;

    public NoOpOutboxMessageSender(ILogger<NoOpOutboxMessageSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Service Bus not configured. Skipping publish for message type {MessageType}", messageType);
        return Task.CompletedTask;
    }
}

public sealed class ServiceBusOutboxMessageSender : IOutboxMessageSender
{
    private readonly ServiceBusSender _sender;

    public ServiceBusOutboxMessageSender(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public async Task SendAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage(payload)
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Subject = messageType,
            ContentType = "application/json"
        };

        await _sender.SendMessageAsync(message, cancellationToken);
    }
}
