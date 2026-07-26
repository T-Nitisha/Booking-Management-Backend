using BookingManagement.Application.Common;
using BookingManagement.Application.Dtos;
using BookingManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingManagement.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AdminManagementService _adminService;

    public DepartmentsController(AdminManagementService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
        => Ok(await _adminService.GetDepartmentsAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAsync([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateDepartmentAsync(request, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateDepartmentAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.ErrorCode, result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminService.DeleteDepartmentAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.ErrorCode, result.Error);
    }

    private IActionResult ToErrorResult(BookingErrorCode? errorCode, string? error) => errorCode switch
    {
        BookingErrorCode.NotFound => NotFound(error),
        _ => BadRequest(error)
    };
}
