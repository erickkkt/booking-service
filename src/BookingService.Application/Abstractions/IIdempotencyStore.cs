namespace BookingService.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}

public sealed record IdempotencyRecord(string Key, string RequestHash, int StatusCode, string ResponseBody, DateTimeOffset CreatedAtUtc);

