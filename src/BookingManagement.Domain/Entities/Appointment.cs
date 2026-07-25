namespace BookingManagement.Domain.Entities;

using BookingManagement.Domain.Common;
using BookingManagement.Domain.Enums;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid SlotId { get; set; }
    public Slot? Slot { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public string? Reason { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public uint Version { get; set; }
}
