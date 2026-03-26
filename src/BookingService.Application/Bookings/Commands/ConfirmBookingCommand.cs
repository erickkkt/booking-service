using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Commands;

public sealed record ConfirmBookingCommand(Guid Id) : IRequest<BookingDto?>;
