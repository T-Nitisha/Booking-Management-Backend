namespace BookingManagement.Application.Dtos;

public record SlotDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string Status);

public record BookAppointmentRequest(Guid SlotId, string? Reason);

public record AppointmentDto(
    Guid Id,
    Guid SlotId,
    Guid PatientId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string Status,
    string? Reason);

public record CancelAppointmentRequest(string? CancellationReason);
