using BookingService.Domain.Bookings;
using FluentAssertions;

namespace BookingService.UnitTests.Bookings;

public sealed class BookingEntityTests
{
    [Fact]
    public void Create_ShouldReturnPendingBookingWithExpectedFields()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");

        booking.CustomerId.Should().Be("CUST-001");
        booking.EventCode.Should().Be("VN-HCM");
        booking.TripCode.Should().Be("SGN-HAN");
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.BookingNumber.Should().StartWith("BK-");
        booking.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        booking.ConfirmedAtUtc.Should().BeNull();
        booking.CancelledAtUtc.Should().BeNull();
        booking.FailedAtUtc.Should().BeNull();
        booking.FailedReason.Should().BeNull();
    }

    [Fact]
    public void Confirm_WhenPending_ShouldTransitionToConfirmed()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");

        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAtUtc.Should().NotBeNull();
        booking.ConfirmedAtUtc!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldBeIdempotent()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        booking.Confirm();
        var firstConfirmedAt = booking.ConfirmedAtUtc;

        // Second confirm should not throw and should not change the timestamp
        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAtUtc.Should().Be(firstConfirmedAt);
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrowInvalidOperationException()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        booking.Cancel();

        var act = () => booking.Confirm();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cancelled*");
    }

    [Fact]
    public void Confirm_WhenFailed_ShouldThrowInvalidOperationException()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        booking.Fail("Inventory unavailable");

        var act = () => booking.Confirm();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed*");
    }

    [Fact]
    public void Cancel_WhenPending_ShouldTransitionToCancelled()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldBeIdempotent()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        booking.Cancel();
        var firstCancelledAt = booking.CancelledAtUtc;

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledAtUtc.Should().Be(firstCancelledAt);
    }

    [Fact]
    public void Fail_WhenPending_ShouldTransitionToFailedAndSetCancelledAt()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");

        booking.Fail("Inventory unavailable");

        booking.Status.Should().Be(BookingStatus.Failed);
        booking.FailedReason.Should().Be("Inventory unavailable");
        booking.FailedAtUtc.Should().NotBeNull();
        // Compensation: CancelledAtUtc should also be set so it cannot be confirmed later
        booking.CancelledAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Fail_WhenAlreadyCancelled_ShouldBeIdempotent()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        booking.Cancel();

        // Calling Fail on an already-cancelled booking should not change status
        booking.Fail("Some error");

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Fail_WhenAlreadyFailed_ShouldBeIdempotent()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        booking.Fail("First reason");

        booking.Fail("Second reason");

        booking.FailedReason.Should().Be("First reason");
    }
}
