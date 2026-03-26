namespace BookingService.Domain.Events;

/// <summary>
/// Published when a booking cannot be fulfilled (e.g., inventory unavailable, payment declined).
/// Edge Case 3: Inventory unavailable after booking — emit BookingFailed, trigger compensation.
/// Consumed by: Notification Service, Inventory Service (release any held seats).
/// </summary>
public sealed record BookingFailedEvent(
    Guid EventId,
    Guid BookingId,
    string BookingNumber,
    string Reason,
    DateTimeOffset FailedAtUtc);
