using BookingService.Application.Abstractions;
using BookingService.Application.Bookings.Commands;
using BookingService.Application.Common;
using BookingService.Domain.Bookings;

namespace BookingService.Application.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IOutboxWriter _outboxWriter;

    public BookingService(IBookingRepository bookingRepository, IOutboxWriter outboxWriter)
    {
        _bookingRepository = bookingRepository;
        _outboxWriter = outboxWriter;
    }

    public async Task<BookingDto> CreateAsync(CreateBookingCommand command, CancellationToken cancellationToken = default)
    {
        var booking = Domain.Bookings.Booking.Create(command.CustomerId, command.EventCode, command.TripCode);

        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _outboxWriter.AddAsync("booking.created", new
        {
            booking.Id,
            booking.BookingNumber,
            booking.CustomerId,
            booking.EventCode,
            booking.TripCode,
            Status = booking.Status.ToString(),
            booking.CreatedAtUtc
        }, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }

    public async Task<IReadOnlyList<BookingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var bookings = await _bookingRepository.GetAllAsync(cancellationToken);
        return bookings.Select(x => x.ToDto()).ToList();
    }

    public async Task<BookingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        return booking?.ToDto();
    }

    public async Task<BookingDto?> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        if (booking is null) return null;

        booking.Confirm();
        await _outboxWriter.AddAsync("booking.confirmed", new { booking.Id, booking.BookingNumber, ConfirmedAtUtc = booking.ConfirmedAtUtc }, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }

    public async Task<BookingDto?> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        if (booking is null) return null;

        booking.Cancel();
        await _outboxWriter.AddAsync("booking.cancelled", new { booking.Id, booking.BookingNumber, CancelledAtUtc = booking.CancelledAtUtc }, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}

internal static class BookingMappings
{
    public static BookingDto ToDto(this Booking booking) => new(
        booking.Id,
        booking.BookingNumber,
        booking.CustomerId,
        booking.EventCode,
        booking.TripCode,
        booking.Status.ToString(),
        booking.CreatedAtUtc,
        booking.ConfirmedAtUtc,
        booking.CancelledAtUtc);
}
