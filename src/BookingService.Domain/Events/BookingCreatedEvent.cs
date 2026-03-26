namespace BookingService.Domain.Events;

/// <summary>
/// Published when a new booking is created.
/// Consumed by: Payment Service, Notification Service, Inventory Service.
/// </summary>
public sealed record BookingCreatedEvent(
    Guid EventId,
    Guid BookingId,
    string BookingNumber,
    string CustomerId,
    string EventCode,
    string TripCode,
    string Status,
    DateTimeOffset CreatedAtUtc);
