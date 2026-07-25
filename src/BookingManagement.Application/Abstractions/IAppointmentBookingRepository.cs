using BookingManagement.Domain.Entities;

namespace BookingManagement.Application.Abstractions;

public interface IAppointmentBookingRepository
{
    Task<Slot?> GetSlotWithAppointmentAsync(Guid slotId, CancellationToken cancellationToken = default);

    Task<List<Slot>> GetAvailableSlotsAsync(Guid doctorId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<Patient?> GetPatientByAuthUserIdAsync(string authUserId, CancellationToken cancellationToken = default);

    /// <summary>Looks up the Patient linked to this Google subject, creating a profile row on first sign-in.</summary>
    Task<Patient> GetOrCreatePatientAsync(string authUserId, string email, string fullName, CancellationToken cancellationToken = default);

    Task<Appointment?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    Task AddAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default);

    /// <summary>Persists tracked changes; throws DbUpdateConcurrencyException on version conflicts.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
