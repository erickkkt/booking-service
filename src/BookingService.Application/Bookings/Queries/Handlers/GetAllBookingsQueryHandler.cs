using BookingService.Application.Abstractions;
using BookingService.Application.Bookings;
using BookingService.Application.Common;
using MediatR;

namespace BookingService.Application.Bookings.Queries.Handlers;

public sealed class GetAllBookingsQueryHandler : IRequestHandler<GetAllBookingsQuery, IReadOnlyList<BookingDto>>
{
    private readonly IBookingRepository _bookingRepository;

    public GetAllBookingsQueryHandler(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<IReadOnlyList<BookingDto>> Handle(GetAllBookingsQuery query, CancellationToken cancellationToken)
    {
        var bookings = await _bookingRepository.GetAllAsync(cancellationToken);
        return bookings.Select(b => b.ToDto()).ToList();
    }
}
