using BookingService.Application.Abstractions;
using BookingService.Application.Bookings;
using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Queries.Handlers;

public sealed class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto?>
{
    private readonly IBookingRepository _bookingRepository;

    public GetBookingByIdQueryHandler(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingDto?> Handle(GetBookingByIdQuery query, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(query.Id, cancellationToken);
        return booking?.ToDto();
    }
}
