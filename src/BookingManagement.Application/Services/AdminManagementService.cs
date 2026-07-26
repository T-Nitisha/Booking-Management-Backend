using BookingManagement.Application.Abstractions;
using BookingManagement.Application.Common;
using BookingManagement.Application.Dtos;
using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;

namespace BookingManagement.Application.Services;

/// <summary>
/// Business logic for admin-managed reference data: departments, doctors, and
/// slot generation. Unlike <see cref="AppointmentBookingService"/> this has no
/// concurrency/locking concerns since only admins mutate this data.
/// </summary>
public class AdminManagementService
{
    private readonly IAdminRepository _repository;

    public AdminManagementService(IAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _repository.GetDepartmentsAsync(cancellationToken);
        return departments.Select(ToDto).ToList();
    }

    public async Task<Result<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = new Department { Name = request.Name, Description = request.Description };
        await _repository.AddDepartmentAsync(department, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<DepartmentDto>.Success(ToDto(department));
    }

    public async Task<Result<DepartmentDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = await _repository.GetDepartmentAsync(id, cancellationToken);
        if (department is null)
        {
            return Result<DepartmentDto>.Failure("Department not found.", BookingErrorCode.NotFound);
        }

        department.Name = request.Name;
        department.Description = request.Description;
        department.UpdatedAtUtc = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<DepartmentDto>.Success(ToDto(department));
    }

    public async Task<Result<bool>> DeleteDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await _repository.GetDepartmentAsync(id, cancellationToken);
        if (department is null)
        {
            return Result<bool>.Failure("Department not found.", BookingErrorCode.NotFound);
        }

        _repository.RemoveDepartment(department);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<List<DoctorDto>> GetDoctorsAsync(CancellationToken cancellationToken = default)
    {
        var doctors = await _repository.GetDoctorsAsync(cancellationToken);
        return doctors.Select(ToDto).ToList();
    }

    public async Task<Result<DoctorDto>> CreateDoctorAsync(CreateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _repository.DepartmentExistsAsync(request.DepartmentId, cancellationToken))
        {
            return Result<DoctorDto>.Failure("Department not found.", BookingErrorCode.NotFound);
        }

        var doctor = new Doctor
        {
            FullName = request.FullName,
            Specialization = request.Specialization,
            Email = request.Email,
            DepartmentId = request.DepartmentId
        };

        await _repository.AddDoctorAsync(doctor, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<DoctorDto>.Success(await ToDtoWithDepartmentAsync(doctor, cancellationToken));
    }

    public async Task<Result<DoctorDto>> UpdateDoctorAsync(Guid id, UpdateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = await _repository.GetDoctorAsync(id, cancellationToken);
        if (doctor is null)
        {
            return Result<DoctorDto>.Failure("Doctor not found.", BookingErrorCode.NotFound);
        }

        if (!await _repository.DepartmentExistsAsync(request.DepartmentId, cancellationToken))
        {
            return Result<DoctorDto>.Failure("Department not found.", BookingErrorCode.NotFound);
        }

        doctor.FullName = request.FullName;
        doctor.Specialization = request.Specialization;
        doctor.Email = request.Email;
        doctor.DepartmentId = request.DepartmentId;
        doctor.UpdatedAtUtc = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<DoctorDto>.Success(await ToDtoWithDepartmentAsync(doctor, cancellationToken));
    }

    public async Task<Result<bool>> DeleteDoctorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doctor = await _repository.GetDoctorAsync(id, cancellationToken);
        if (doctor is null)
        {
            return Result<bool>.Failure("Doctor not found.", BookingErrorCode.NotFound);
        }

        _repository.RemoveDoctor(doctor);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<int>> GenerateSlotsAsync(GenerateSlotsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ToDate < request.FromDate || request.EndHourUtc <= request.StartHourUtc)
        {
            return Result<int>.Failure("Invalid date/hour range.", BookingErrorCode.Forbidden);
        }

        var doctor = await _repository.GetDoctorAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result<int>.Failure("Doctor not found.", BookingErrorCode.NotFound);
        }

        var slots = new List<Slot>();
        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            for (var hour = request.StartHourUtc; hour < request.EndHourUtc; hour++)
            {
                var start = date.ToDateTime(new TimeOnly(hour, 0), DateTimeKind.Utc);
                slots.Add(new Slot
                {
                    DoctorId = request.DoctorId,
                    StartTimeUtc = start,
                    EndTimeUtc = start.AddHours(1),
                    Status = SlotStatus.Available
                });
            }
        }

        await _repository.AddSlotsAsync(slots, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(slots.Count);
    }

    public async Task<Result<bool>> DeleteSlotAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var slot = await _repository.GetSlotAsync(id, cancellationToken);
        if (slot is null)
        {
            return Result<bool>.Failure("Slot not found.", BookingErrorCode.NotFound);
        }

        if (slot.Status == SlotStatus.Booked)
        {
            return Result<bool>.Failure("Cannot delete a booked slot; cancel the appointment first.", BookingErrorCode.Forbidden);
        }

        _repository.RemoveSlot(slot);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static DepartmentDto ToDto(Department department) =>
        new(department.Id, department.Name, department.Description);

    private static DoctorDto ToDto(Doctor doctor) =>
        new(doctor.Id, doctor.FullName, doctor.Specialization, doctor.Email, doctor.DepartmentId,
            doctor.Department?.Name ?? string.Empty, doctor.AuthUserId);

    private async Task<DoctorDto> ToDtoWithDepartmentAsync(Doctor doctor, CancellationToken cancellationToken)
    {
        if (doctor.Department is null)
        {
            doctor.Department = await _repository.GetDepartmentAsync(doctor.DepartmentId, cancellationToken);
        }

        return ToDto(doctor);
    }
}
