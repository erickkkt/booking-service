using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BookingService.Application.Bookings.Commands;
using BookingService.Domain.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Messaging;

/// <summary>
/// Edge Case 1: Payment success but booking not updated.
/// Fix: eventual consistency + retry via Azure Service Bus.
/// This hosted service subscribes to PaymentCompleted events and calls ConfirmBookingCommand.
/// If the booking service crashes mid-way, the message is redelivered by Service Bus (at-least-once delivery).
/// The ConfirmBookingCommand is idempotent: if already Confirmed, it's a no-op.
/// </summary>
public sealed class PaymentCompletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentCompletedConsumer> _logger;
    private readonly ServiceBusClient? _serviceBusClient;
    private readonly string _subscriptionName;
    private readonly string _topicName;

    public PaymentCompletedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentCompletedConsumer> logger,
        ServiceBusClient? serviceBusClient,
        string topicName,
        string subscriptionName)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _serviceBusClient = serviceBusClient;
        _topicName = topicName;
        _subscriptionName = subscriptionName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_serviceBusClient is null)
        {
            _logger.LogInformation("Service Bus not configured. PaymentCompletedConsumer is inactive.");
            return;
        }

        await using var processor = _serviceBusClient.CreateProcessor(
            _topicName,
            _subscriptionName,
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });

        processor.ProcessMessageAsync += OnMessageReceivedAsync;
        processor.ProcessErrorAsync += OnErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            await processor.StopProcessingAsync();
        }
    }

    private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentCompletedEvent>(args.Message.Body.ToString());
            if (@event is null)
            {
                _logger.LogWarning("Received null PaymentCompletedEvent. Skipping.");
                await args.DeadLetterMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                return;
            }

            _logger.LogInformation(
                "Processing PaymentCompletedEvent {EventId} for booking {BookingId}",
                @event.EventId, @event.BookingId);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Confirm the booking — idempotent: if already Confirmed this is a no-op
            await mediator.Send(new ConfirmBookingCommand(@event.BookingId), args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} confirmed via PaymentCompletedEvent {EventId}",
                @event.BookingId, @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PaymentCompletedEvent. Message will be retried.");
            // Do not complete — Service Bus will redeliver (at-least-once, retry-safe)
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor error on {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }
}
