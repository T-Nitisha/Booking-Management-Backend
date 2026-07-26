using BookingManagement.Application.Common;
using BookingManagement.Application.Dtos;
using BookingManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingManagement.Api.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly AdminManagementService _adminService;

    public DoctorsController(AdminManagementService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
        => Ok(await _adminService.GetDoctorsAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateDoctorRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateDoctorAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.ErrorCode, result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateDoctorRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateDoctorAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.ErrorCode, result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminService.DeleteDoctorAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.ErrorCode, result.Error);
    }

    private IActionResult ToErrorResult(BookingErrorCode? errorCode, string? error) => errorCode switch
    {
        BookingErrorCode.NotFound => NotFound(error),
        _ => BadRequest(error)
    };
}
