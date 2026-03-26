using BookingService.Application.Abstractions;
using BookingService.Application.Common;
using BookingService.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Bookings.Commands.Handlers;

public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, BookingDto?>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly ILogger<CancelBookingCommandHandler> _logger;

    public CancelBookingCommandHandler(
        IBookingRepository bookingRepository,
        IOutboxWriter outboxWriter,
        ILogger<CancelBookingCommandHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _outboxWriter = outboxWriter;
        _logger = logger;
    }

    public async Task<BookingDto?> Handle(CancelBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(command.Id, cancellationToken);
        if (booking is null)
            return null;

        booking.Cancel();

        await _outboxWriter.AddAsync("booking.cancelled", new BookingCancelledEvent(
            EventId: Guid.NewGuid(),
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            CancelledAtUtc: booking.CancelledAtUtc!.Value), cancellationToken);

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} ({BookingNumber}) cancelled",
            booking.Id, booking.BookingNumber);

        return booking.ToDto();
    }
}
