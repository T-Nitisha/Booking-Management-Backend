namespace BookingManagement.Domain.Entities;

using BookingManagement.Domain.Common;

public class Doctor : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Links to the identity provider's subject (OIDC "sub" claim), same as Patient.
    /// </summary>
    public string? AuthUserId { get; set; }

    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<Slot> Slots { get; set; } = new List<Slot>();
}
