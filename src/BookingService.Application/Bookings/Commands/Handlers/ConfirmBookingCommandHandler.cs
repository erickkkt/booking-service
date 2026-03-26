using BookingService.Application.Abstractions;
using BookingService.Application.Common;
using BookingService.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Bookings.Commands.Handlers;

public sealed class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, BookingDto?>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly ILogger<ConfirmBookingCommandHandler> _logger;

    public ConfirmBookingCommandHandler(
        IBookingRepository bookingRepository,
        IOutboxWriter outboxWriter,
        ILogger<ConfirmBookingCommandHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _outboxWriter = outboxWriter;
        _logger = logger;
    }

    public async Task<BookingDto?> Handle(ConfirmBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(command.Id, cancellationToken);
        if (booking is null)
            return null;

        booking.Confirm();

        await _outboxWriter.AddAsync("booking.confirmed", new BookingConfirmedEvent(
            EventId: Guid.NewGuid(),
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            ConfirmedAtUtc: booking.ConfirmedAtUtc!.Value), cancellationToken);

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} ({BookingNumber}) confirmed",
            booking.Id, booking.BookingNumber);

        return booking.ToDto();
    }
}
