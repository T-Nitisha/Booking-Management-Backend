namespace BookingManagement.Domain.Entities;

using BookingManagement.Domain.Common;
using BookingManagement.Domain.Enums;

/// <summary>
/// A bookable time window offered by a doctor. Concurrency-sensitive: many patients
/// may attempt to book the same slot simultaneously, so <see cref="Version"/> backs
/// optimistic concurrency checks (mapped to Postgres xmin or an explicit column).
/// </summary>
public class Slot : BaseEntity
{
    public Guid DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public SlotStatus Status { get; set; } = SlotStatus.Available;

    /// <summary>Optimistic concurrency token.</summary>
    public uint Version { get; set; }

    public Appointment? Appointment { get; set; }
}
