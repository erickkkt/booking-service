using BookingService.Application.Bookings.Commands;
using BookingService.Application.Bookings.Validators;
using FluentAssertions;
using FluentValidation;

namespace BookingService.UnitTests.Bookings;

public sealed class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator = new();

    [Theory]
    [InlineData("CUST-001", "VN-HCM", "SGN-HAN")]
    [InlineData("USER-ABC", "EVENT-X", "TRIP-Y")]
    public void Validate_WhenAllFieldsValid_ShouldPass(string customerId, string eventCode, string tripCode)
    {
        var command = new CreateBookingCommand(customerId, eventCode, tripCode);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "VN-HCM", "SGN-HAN", "CustomerId")]
    [InlineData("CUST-001", "", "SGN-HAN", "EventCode")]
    [InlineData("CUST-001", "VN-HCM", "", "TripCode")]
    public void Validate_WhenRequiredFieldEmpty_ShouldFail(string customerId, string eventCode, string tripCode, string expectedField)
    {
        var command = new CreateBookingCommand(customerId, eventCode, tripCode);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == expectedField);
    }

    [Fact]
    public void Validate_WhenCustomerIdExceedsMaxLength_ShouldFail()
    {
        var longCustomerId = new string('A', 65);
        var command = new CreateBookingCommand(longCustomerId, "VN-HCM", "SGN-HAN");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "CustomerId");
    }
}
