using BookingService.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BookingNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EventCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TripCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.BookingNumber).IsUnique();
    }
}
