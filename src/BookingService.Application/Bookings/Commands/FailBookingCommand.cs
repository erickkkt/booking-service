using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Commands;

/// <summary>
/// Edge Case 3: Inventory unavailable after booking.
/// Fix: emit BookingFailed event and trigger compensation (cancel).
/// </summary>
public sealed record FailBookingCommand(Guid Id, string Reason) : IRequest<BookingDto?>;
