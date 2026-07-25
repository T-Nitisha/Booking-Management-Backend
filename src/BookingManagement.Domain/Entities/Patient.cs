namespace BookingManagement.Domain.Entities;

using BookingManagement.Domain.Common;

public class Patient : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Links to the identity provider's subject (OIDC "sub" claim).
    /// </summary>
    public string? AuthUserId { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
