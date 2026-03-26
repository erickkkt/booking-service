using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Queries;

public sealed record GetBookingByIdQuery(Guid Id) : IRequest<BookingDto?>;

