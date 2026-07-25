using System.Security.Claims;
using BookingManagement.Domain.Enums;
using BookingManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BookingManagement.Infrastructure.Auth;

/// <summary>
/// After Google's JWT is validated, resolves the caller's application role
/// (Patient / Doctor / Admin) from our own database and attaches it as a
/// standard <see cref="ClaimTypes.Role"/> claim so [Authorize(Roles=...)]
/// works the same way it would with a full OIDC provider.
/// </summary>
public class AppRoleClaimsTransformation : IClaimsTransformation
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AppRoleClaimsTransformation(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated || identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return principal;
        }

        var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return principal;
        }

        var role = await ResolveRoleAsync(subject, email);
        if (role is not null)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Value.ToString()));
        }

        return principal;
    }

    private async Task<AppRole?> ResolveRoleAsync(string subject, string? email)
    {
        var adminEmails = _configuration.GetSection("Authorization:AdminEmails").Get<string[]>() ?? [];
        if (!string.IsNullOrWhiteSpace(email) && adminEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            return AppRole.Admin;
        }

        var isDoctor = await _dbContext.Doctors.AnyAsync(d => d.AuthUserId == subject);
        if (isDoctor)
        {
            return AppRole.Doctor;
        }

        var isPatient = await _dbContext.Patients.AnyAsync(p => p.AuthUserId == subject);
        if (isPatient)
        {
            return AppRole.Patient;
        }

        // First-time Google sign-in with no matching record yet: default to
        // Patient, the Patients repository will upsert a profile row on first use.
        return AppRole.Patient;
    }
}
