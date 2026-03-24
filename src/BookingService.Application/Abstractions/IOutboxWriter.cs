namespace BookingService.Application.Abstractions;

public interface IOutboxWriter
{
    Task AddAsync<T>(string type, T payload, CancellationToken cancellationToken = default);
}
