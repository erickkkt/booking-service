using BookingService.Application.Abstractions;
using BookingService.Application.Common;
using BookingService.Domain.Bookings;
using BookingService.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Bookings.Commands.Handlers;

public sealed class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    public CreateBookingCommandHandler(
        IBookingRepository bookingRepository,
        IOutboxWriter outboxWriter,
        ILogger<CreateBookingCommandHandler> logger)
    {
        _bookingRepository = bookingRepository;
        _outboxWriter = outboxWriter;
        _logger = logger;
    }

    public async Task<BookingDto> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = Booking.Create(command.CustomerId, command.EventCode, command.TripCode);

        await _bookingRepository.AddAsync(booking, cancellationToken);

        await _outboxWriter.AddAsync("booking.created", new BookingCreatedEvent(
            EventId: Guid.NewGuid(),
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            CustomerId: booking.CustomerId,
            EventCode: booking.EventCode,
            TripCode: booking.TripCode,
            Status: booking.Status.ToString(),
            CreatedAtUtc: booking.CreatedAtUtc), cancellationToken);

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} ({BookingNumber}) created for customer {CustomerId}",
            booking.Id, booking.BookingNumber, booking.CustomerId);

        return booking.ToDto();
    }
}
