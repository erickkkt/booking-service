namespace BookingService.Domain.Events;

/// <summary>
/// Published when a booking is cancelled by the user or system.
/// Consumed by: Notification Service, Inventory Service (release seats), Payment Service (refund).
/// </summary>
public sealed record BookingCancelledEvent(
    Guid EventId,
    Guid BookingId,
    string BookingNumber,
    DateTimeOffset CancelledAtUtc);
