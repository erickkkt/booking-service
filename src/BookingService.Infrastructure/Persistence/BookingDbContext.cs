using BookingService.Domain.Bookings;
using BookingService.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Persistence;

public sealed class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyEntry> IdempotencyEntries => Set<IdempotencyEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}

public sealed class IdempotencyEntry
{
    public string Key { get; set; } = default!;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
