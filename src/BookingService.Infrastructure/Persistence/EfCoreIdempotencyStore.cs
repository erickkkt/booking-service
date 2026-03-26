using BookingService.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Persistence;

public sealed class EfCoreIdempotencyStore : IIdempotencyStore
{
    private readonly BookingDbContext _dbContext;

    public EfCoreIdempotencyStore(BookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.IdempotencyEntries.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        return entity is null
            ? null
            : new IdempotencyRecord(entity.Key, entity.RequestHash, entity.StatusCode, entity.ResponseBody, entity.CreatedAtUtc);
    }

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.IdempotencyEntries.FirstOrDefaultAsync(x => x.Key == record.Key, cancellationToken);
        if (entity is null)
        {
            _dbContext.IdempotencyEntries.Add(new IdempotencyEntry
            {
                Key = record.Key,
                RequestHash = record.RequestHash,
                StatusCode = record.StatusCode,
                ResponseBody = record.ResponseBody,
                CreatedAtUtc = record.CreatedAtUtc
            });
        }
        else
        {
            entity.RequestHash = record.RequestHash;
            entity.StatusCode = record.StatusCode;
            entity.ResponseBody = record.ResponseBody;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

