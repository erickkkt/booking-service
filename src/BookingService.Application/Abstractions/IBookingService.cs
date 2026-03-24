using BookingService.Application.Bookings.Commands;
using BookingService.Application.Common;

namespace BookingService.Application.Abstractions;

public interface IBookingService
{
    Task<BookingDto> CreateAsync(CreateBookingCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BookingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingDto?> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingDto?> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
