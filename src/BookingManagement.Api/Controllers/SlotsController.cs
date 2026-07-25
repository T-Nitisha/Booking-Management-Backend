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

    public SlotsController(AppointmentBookingService bookingService)
    {
        _bookingService = bookingService;
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
}
