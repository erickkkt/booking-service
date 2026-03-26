using BookingService.Application.Common;
using BookingService.Domain.Bookings;

namespace BookingService.Application.Bookings;

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
        booking.CancelledAtUtc,
        booking.FailedAtUtc,
        booking.FailedReason);
}
