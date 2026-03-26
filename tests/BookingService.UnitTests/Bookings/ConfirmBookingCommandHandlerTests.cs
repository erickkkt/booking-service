using BookingService.Application.Abstractions;
using BookingService.Application.Bookings.Commands;
using BookingService.Application.Bookings.Commands.Handlers;
using BookingService.Domain.Bookings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BookingService.UnitTests.Bookings;

public sealed class ConfirmBookingCommandHandlerTests
{
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly ConfirmBookingCommandHandler _handler;

    public ConfirmBookingCommandHandlerTests()
    {
        _handler = new ConfirmBookingCommandHandler(
            _bookingRepository,
            _outboxWriter,
            NullLogger<ConfirmBookingCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenBookingExists_ShouldConfirmAndPublishEvent()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        _bookingRepository.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Confirmed");
        result.ConfirmedAtUtc.Should().NotBeNull();

        await _outboxWriter.Received(1).AddAsync("booking.confirmed", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _bookingRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBookingNotFound_ShouldReturnNull()
    {
        _bookingRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Booking?)null);

        var result = await _handler.Handle(new ConfirmBookingCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
        await _outboxWriter.DidNotReceive().AddAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_ShouldBeIdempotent()
    {
        var booking = Booking.Create("CUST-001", "VN-HCM", "SGN-HAN");
        booking.Confirm(); // already confirmed
        _bookingRepository.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        // Should not throw
        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Confirmed");
    }
}
