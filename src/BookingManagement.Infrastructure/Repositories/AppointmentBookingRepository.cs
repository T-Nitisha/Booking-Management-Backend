using BookingManagement.Application.Abstractions;
using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;
using BookingManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingManagement.Infrastructure.Repositories;

public class AppointmentBookingRepository : IAppointmentBookingRepository
{
    private readonly AppDbContext _dbContext;

    public AppointmentBookingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Slot?> GetSlotWithAppointmentAsync(Guid slotId, CancellationToken cancellationToken = default)
        => _dbContext.Slots
            .Include(s => s.Doctor)
            .Include(s => s.Appointment)
            .FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);

    public Task<List<Slot>> GetAvailableSlotsAsync(Guid doctorId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        => _dbContext.Slots
            .Include(s => s.Doctor)
            .Where(s => s.DoctorId == doctorId
                && s.Status == SlotStatus.Available
                && s.StartTimeUtc >= fromUtc
                && s.StartTimeUtc < toUtc)
            .OrderBy(s => s.StartTimeUtc)
            .ToListAsync(cancellationToken);

    public Task<Patient?> GetPatientByAuthUserIdAsync(string authUserId, CancellationToken cancellationToken = default)
        => _dbContext.Patients.FirstOrDefaultAsync(p => p.AuthUserId == authUserId, cancellationToken);

    public async Task<Patient> GetOrCreatePatientAsync(string authUserId, string email, string fullName, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.AuthUserId == authUserId, cancellationToken);
        if (patient is not null)
        {
            return patient;
        }

        patient = new Patient
        {
            AuthUserId = authUserId,
            Email = email,
            FullName = fullName
        };

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return patient;
    }

    public Task<Appointment?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        => _dbContext.Appointments
            .Include(a => a.Slot)!.ThenInclude(s => s!.Doctor)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

    public Task AddAppointmentAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _dbContext.Appointments.Add(appointment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
