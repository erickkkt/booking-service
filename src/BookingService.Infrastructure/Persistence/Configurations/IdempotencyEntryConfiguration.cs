using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyEntryConfiguration : IEntityTypeConfiguration<IdempotencyEntry>
{
    public void Configure(EntityTypeBuilder<IdempotencyEntry> builder)
    {
        builder.ToTable("idempotency_entries");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(128);
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResponseBody).HasColumnType("text").IsRequired();
    }
}
