namespace BookingManagement.NotificationWorker.Models;

/// <summary>Deserialized shape of the JSON payload published by the Api's KafkaBookingEventPublisher.</summary>
public record BookingEventMessage(
    Guid Id,
    Guid SlotId,
    Guid PatientId,
    string Status,
    Guid? DoctorId,
    string? DoctorName,
    DateTime? StartTimeUtc,
    string? PatientEmail,
    string? PatientName,
    DateTime OccurredAtUtc);
