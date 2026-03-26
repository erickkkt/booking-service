namespace BookingService.Domain.Events;

/// <summary>
/// Published by the Payment Service and consumed by the Booking Service.
/// Edge Case 1: Payment success but booking not updated
/// → Fix: eventual consistency + retry via Outbox/Service Bus.
/// The Booking Service processes this event and transitions the booking to Confirmed.
/// </summary>
public sealed record PaymentCompletedEvent(
    Guid EventId,
    Guid BookingId,
    string BookingNumber,
    decimal Amount,
    string Currency,
    DateTimeOffset PaidAtUtc);
