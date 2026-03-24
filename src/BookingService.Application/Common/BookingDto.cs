namespace BookingService.Application.Common;

public sealed record BookingDto(
    Guid Id,
    string BookingNumber,
    string CustomerId,
    string EventCode,
    string TripCode,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CancelledAtUtc);
