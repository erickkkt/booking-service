using System.Text.Json;
using BookingService.Application.Abstractions;
using BookingService.Domain.Outbox;
using BookingService.Infrastructure.Persistence;

namespace BookingService.Infrastructure.Outbox;

public sealed class EfCoreOutboxWriter : IOutboxWriter
{
    private readonly BookingDbContext _dbContext;

    public EfCoreOutboxWriter(BookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync<T>(string type, T payload, CancellationToken cancellationToken = default)
    {
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }
}
