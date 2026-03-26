using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Queries;

public sealed record GetAllBookingsQuery : IRequest<IReadOnlyList<BookingDto>>;
