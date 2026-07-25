namespace BookingManagement.Domain.Entities;

using BookingManagement.Domain.Common;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
