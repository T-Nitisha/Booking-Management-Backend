using BookingManagement.Domain.Entities;

namespace BookingManagement.Application.Abstractions;

/// <summary>
/// Admin CRUD operations for reference/scheduling data (departments, doctors, slots).
/// Kept separate from <see cref="IAppointmentBookingRepository"/>, which is
/// scoped to the booking/concurrency-sensitive flow.
/// </summary>
public interface IAdminRepository
{
    Task<List<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddDepartmentAsync(Department department, CancellationToken cancellationToken = default);

    Task<List<Doctor>> GetDoctorsAsync(CancellationToken cancellationToken = default);

    Task<Doctor?> GetDoctorAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddDoctorAsync(Doctor doctor, CancellationToken cancellationToken = default);

    Task<bool> DepartmentExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddSlotsAsync(IEnumerable<Slot> slots, CancellationToken cancellationToken = default);

    Task<Slot?> GetSlotAsync(Guid id, CancellationToken cancellationToken = default);

    void RemoveSlot(Slot slot);

    void RemoveDepartment(Department department);

    void RemoveDoctor(Doctor doctor);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
