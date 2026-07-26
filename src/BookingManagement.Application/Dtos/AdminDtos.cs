namespace BookingManagement.Application.Dtos;

public record DepartmentDto(Guid Id, string Name, string Description);

public record CreateDepartmentRequest(string Name, string Description);

public record UpdateDepartmentRequest(string Name, string Description);

public record DoctorDto(
    Guid Id,
    string FullName,
    string Specialization,
    string Email,
    Guid DepartmentId,
    string DepartmentName,
    string? AuthUserId);

public record CreateDoctorRequest(string FullName, string Specialization, string Email, Guid DepartmentId);

public record UpdateDoctorRequest(string FullName, string Specialization, string Email, Guid DepartmentId);

/// <summary>Generates one slot per hour between StartHour (inclusive) and EndHour (exclusive), UTC, for each day in the range.</summary>
public record GenerateSlotsRequest(Guid DoctorId, DateOnly FromDate, DateOnly ToDate, int StartHourUtc, int EndHourUtc);

public record CreateSlotRequest(Guid DoctorId, DateTime StartTimeUtc, DateTime EndTimeUtc);
