using BookingService.Application.Bookings.Commands;
using FluentValidation;

namespace BookingService.Application.Bookings.Validators;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required.")
            .MaximumLength(64).WithMessage("CustomerId must not exceed 64 characters.");

        RuleFor(x => x.EventCode)
            .NotEmpty().WithMessage("EventCode is required.")
            .MaximumLength(64).WithMessage("EventCode must not exceed 64 characters.");

        RuleFor(x => x.TripCode)
            .NotEmpty().WithMessage("TripCode is required.")
            .MaximumLength(64).WithMessage("TripCode must not exceed 64 characters.");
    }
}
