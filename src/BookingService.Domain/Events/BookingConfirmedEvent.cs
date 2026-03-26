namespace BookingService.Domain.Events;

/// <summary>
/// Published when a booking transitions to Confirmed status.
/// Consumed by: Notification Service, Payment Service.
/// </summary>
public sealed record BookingConfirmedEvent(
    Guid EventId,
    Guid BookingId,
    string BookingNumber,
    DateTimeOffset ConfirmedAtUtc);
