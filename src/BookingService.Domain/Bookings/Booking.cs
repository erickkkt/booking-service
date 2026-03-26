namespace BookingService.Domain.Bookings;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public string BookingNumber { get; private set; } = default!;
    public string CustomerId { get; private set; } = default!;
    public string EventCode { get; private set; } = default!;
    public string TripCode { get; private set; } = default!;
    public BookingStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public string? FailedReason { get; private set; }

    private Booking() { }

    private Booking(Guid id, string bookingNumber, string customerId, string eventCode, string tripCode)
    {
        Id = id;
        BookingNumber = bookingNumber;
        CustomerId = customerId;
        EventCode = eventCode;
        TripCode = tripCode;
        Status = BookingStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Booking Create(string customerId, string eventCode, string tripCode)
    {
        return new Booking(Guid.NewGuid(), $"BK-{Random.Shared.Next(100000, 999999)}", customerId, eventCode, tripCode);
    }

    public void Confirm()
    {
        if (Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Cancelled bookings cannot be confirmed.");

        if (Status == BookingStatus.Failed)
            throw new InvalidOperationException("Failed bookings cannot be confirmed.");

        if (Status == BookingStatus.Confirmed)
            return;

        Status = BookingStatus.Confirmed;
        ConfirmedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            return;

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the booking as failed (e.g. inventory unavailable, payment failure).
    /// Triggers a compensation flow: the booking is also cancelled.
    /// Edge Case 3: Inventory unavailable after booking — emit BookingFailed, trigger compensation.
    /// </summary>
    public void Fail(string reason)
    {
        if (Status is BookingStatus.Cancelled or BookingStatus.Failed)
            return;

        Status = BookingStatus.Failed;
        FailedReason = reason;
        FailedAtUtc = DateTimeOffset.UtcNow;
        // Compensation: auto-cancel so the booking cannot be confirmed later
        CancelledAtUtc = DateTimeOffset.UtcNow;
    }
}
