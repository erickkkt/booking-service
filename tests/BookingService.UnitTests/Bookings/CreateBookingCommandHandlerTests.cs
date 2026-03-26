using BookingService.Application.Abstractions;
using BookingService.Application.Bookings.Commands;
using BookingService.Application.Bookings.Commands.Handlers;
using BookingService.Application.Common;
using BookingService.Domain.Bookings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BookingService.UnitTests.Bookings;

public sealed class CreateBookingCommandHandlerTests
{
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingCommandHandlerTests()
    {
        _handler = new CreateBookingCommandHandler(
            _bookingRepository,
            _outboxWriter,
            NullLogger<CreateBookingCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldCreateBookingAndPublishEvent()
    {
        var command = new CreateBookingCommand("CUST-001", "VN-HCM", "SGN-HAN");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be("CUST-001");
        result.EventCode.Should().Be("VN-HCM");
        result.TripCode.Should().Be("SGN-HAN");
        result.Status.Should().Be("Pending");
        result.BookingNumber.Should().StartWith("BK-");

        await _bookingRepository.Received(1).AddAsync(Arg.Any<Booking>(), Arg.Any<CancellationToken>());
        await _outboxWriter.Received(1).AddAsync("booking.created", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _bookingRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBookingDtoWithCorrectFields()
    {
        var command = new CreateBookingCommand("CUST-XYZ", "EVENT-1", "TRIP-A");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.ConfirmedAtUtc.Should().BeNull();
        result.CancelledAtUtc.Should().BeNull();
        result.FailedAtUtc.Should().BeNull();
    }
}
