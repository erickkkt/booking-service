using BookingService.Application.Abstractions;
using BookingService.Application.Common;
using BookingService.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Bookings.Commands.Handlers;

/// <summary>
/// Edge Case 3: Inventory unavailable after booking.
/// Marks the booking as Failed, emits BookingFailedEvent, and triggers compensation
/// by also emitting BookingCancelledEvent so downstream services (Inventory, Notification) can react.
/// </summary>
public sealed class FailBookingCommandHandler : IRequestHandler<FailBookingCommand, BookingDto?>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly ILogger<FailBookingCommandHandler> _logger;

    public FailBookingCommandHandler(
        IBookingRepository bookingRepository,
        IOutboxWriter outboxWriter,
        ILogger<FailBookingCommandHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _outboxWriter = outboxWriter;
        _logger = logger;
    }

    public async Task<BookingDto?> Handle(FailBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(command.Id, cancellationToken);
        if (booking is null)
            return null;

        booking.Fail(command.Reason);

        // Emit BookingFailed so Notification Service and Inventory Service can react
        await _outboxWriter.AddAsync("booking.failed", new BookingFailedEvent(
            EventId: Guid.NewGuid(),
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            Reason: command.Reason,
            FailedAtUtc: booking.FailedAtUtc!.Value), cancellationToken);

        // Compensation: emit BookingCancelled so Inventory Service releases reserved seats
        await _outboxWriter.AddAsync("booking.cancelled", new BookingCancelledEvent(
            EventId: Guid.NewGuid(),
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            CancelledAtUtc: booking.CancelledAtUtc!.Value), cancellationToken);

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Booking {BookingId} ({BookingNumber}) failed. Reason: {Reason}",
            booking.Id, booking.BookingNumber, command.Reason);

        return booking.ToDto();
    }
}
