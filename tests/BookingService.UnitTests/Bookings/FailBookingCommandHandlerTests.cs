using BookingService.Application.Abstractions;
using BookingService.Application.Bookings.Commands;
using BookingService.Application.Bookings.Commands.Handlers;
using BookingService.Domain.Bookings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BookingService.UnitTests.Bookings;

/// <summary>
/// Edge Case 3: Inventory unavailable after booking.
/// The FailBookingCommandHandler should emit BookingFailed and BookingCancelled (compensation).
/// </summary>
public sealed class FailBookingCommandHandlerTests
{
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly FailBookingCommandHandler _handler;

    public FailBookingCommandHandlerTests()
    {
        _handler = new FailBookingCommandHandler(
            _bookingRepository,
            _outboxWriter,
            NullLogger<FailBookingCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenBookingExists_ShouldFailAndEmitTwoEvents()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        _bookingRepository.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        var result = await _handler.Handle(
            new FailBookingCommand(booking.Id, "Inventory unavailable"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Failed");
        result.FailedReason.Should().Be("Inventory unavailable");
        result.FailedAtUtc.Should().NotBeNull();
        result.CancelledAtUtc.Should().NotBeNull();

        // Must emit both BookingFailed (for notifications) and BookingCancelled (compensation)
        await _outboxWriter.Received(1).AddAsync("booking.failed", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _outboxWriter.Received(1).AddAsync("booking.cancelled", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _bookingRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBookingNotFound_ShouldReturnNull()
    {
        _bookingRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Booking?)null);

        var result = await _handler.Handle(
            new FailBookingCommand(Guid.NewGuid(), "Some error"),
            CancellationToken.None);

        result.Should().BeNull();
        await _outboxWriter.DidNotReceive().AddAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
