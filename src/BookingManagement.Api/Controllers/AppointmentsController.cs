using System.Security.Claims;
using BookingManagement.Application.Common;
using BookingManagement.Application.Dtos;
using BookingManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingManagement.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentBookingService _bookingService;

    public AppointmentsController(AppointmentBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> BookAsync([FromBody] BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var authUserId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;
        var fullName = User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Name) ?? email;

        if (string.IsNullOrWhiteSpace(authUserId))
        {
            return Unauthorized();
        }

        var result = await _bookingService.BookAppointmentAsync(authUserId, email, fullName, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid appointmentId, [FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CancelAppointmentAsync(appointmentId, request.CancellationReason ?? string.Empty, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result<AppointmentDto> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            BookingErrorCode.NotFound => NotFound(new { error = result.Error }),
            BookingErrorCode.SlotAlreadyBooked => Conflict(new { error = result.Error }),
            BookingErrorCode.ConcurrencyConflict => Conflict(new { error = result.Error }),
            BookingErrorCode.LockTimeout => StatusCode(StatusCodes.Status423Locked, new { error = result.Error }),
            BookingErrorCode.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
