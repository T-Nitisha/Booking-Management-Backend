using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingManagement.Api.Controllers;

/// <summary>
/// Exposes the caller's resolved application role (Patient/Doctor/Admin) so the
/// frontend can decide what to show (e.g. the admin section) without decoding
/// server-only claims itself — the Google ID token never contains this role.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Patient";

        return Ok(new { email, name, role });
    }
}
