using BookingManagement.Application.Common;
using BookingManagement.Application.Dtos;
using BookingManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingManagement.Api.Controllers;

[ApiController]
[Route("api/slots")]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly AppointmentBookingService _bookingService;
    private readonly AdminManagementService _adminService;

    public SlotsController(AppointmentBookingService bookingService, AdminManagementService adminService)
    {
        _bookingService = bookingService;
        _adminService = adminService;
    }

    /// <summary>Search available (unbooked) slots for a doctor within a UTC date range.</summary>
    [HttpGet]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] Guid doctorId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (doctorId == Guid.Empty || toUtc <= fromUtc)
        {
            return BadRequest("doctorId is required and toUtc must be after fromUtc.");
        }

        var slots = await _bookingService.SearchAvailableSlotsAsync(doctorId, fromUtc, toUtc, cancellationToken);
        return Ok(slots);
    }

    /// <summary>Bulk-generates hourly slots for a doctor across a date range (e.g. a week's schedule).</summary>
    [HttpPost("generate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateAsync([FromBody] GenerateSlotsRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.GenerateSlotsAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(new { created = result.Value })
            : ToErrorResult(result.ErrorCode, result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminService.DeleteSlotAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.ErrorCode, result.Error);
    }

    private IActionResult ToErrorResult(BookingErrorCode? errorCode, string? error) => errorCode switch
    {
        BookingErrorCode.NotFound => NotFound(error),
        _ => BadRequest(error)
    };
}
