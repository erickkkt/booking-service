using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Commands;

public sealed record CancelBookingCommand(Guid Id) : IRequest<BookingDto?>;
