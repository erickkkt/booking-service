using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Commands;

public sealed record CreateBookingCommand(string CustomerId, string EventCode, string TripCode)
    : IRequest<BookingDto>;

