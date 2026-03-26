using BookingService.Application.Bookings.Commands;
using BookingService.Application.Bookings.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var bookings = await _mediator.Send(new GetAllBookingsQuery(), cancellationToken);
        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _mediator.Send(new GetBookingByIdQuery(id), cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _mediator.Send(new CreateBookingCommand(request.CustomerId, request.EventCode, request.TripCode), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(
                ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }
    }

    /// <summary>
    /// Confirm a pending booking.
    /// Edge Case 1: If payment completes (PaymentCompletedEvent) this endpoint is also called
    /// by the internal PaymentCompletedConsumer to confirm the booking (eventual consistency).
    /// </summary>
    [HttpPut("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new ConfirmBookingCommand(id), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Cancel a booking. Edge Case 2: duplicate requests are handled by the Idempotency-Key middleware.
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new CancelBookingCommand(id), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Fail a booking due to inventory unavailability or another system error.
    /// Edge Case 3: emits BookingFailedEvent + BookingCancelledEvent (compensation).
    /// </summary>
    [HttpPut("{id:guid}/fail")]
    public async Task<IActionResult> Fail(Guid id, [FromBody] FailBookingRequest request, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new FailBookingCommand(id, request.Reason), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
}

public sealed record CreateBookingRequest(string CustomerId, string EventCode, string TripCode);

public sealed record FailBookingRequest(string Reason);

