using BookingService.Application.Abstractions;
using BookingService.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Persistence;

public sealed class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _dbContext;

    public BookingRepository(BookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Bookings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Bookings.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
        => await _dbContext.Bookings.AddAsync(booking, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}
