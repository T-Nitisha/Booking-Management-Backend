namespace BookingManagement.NotificationWorker.Messaging;

/// <summary>
/// Mirrors BookingManagement.Infrastructure.Messaging.KafkaOptions in the Api
/// project. Kept as a separate copy (rather than a shared project reference) so
/// this worker can be built and deployed completely independently, as its own
/// Render Background Worker service.
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = string.Empty;
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;
    public string GroupId { get; set; } = "notification-worker";
    public string AppointmentBookedTopic { get; set; } = "appointment-booked";
    public string AppointmentCancelledTopic { get; set; } = "appointment-cancelled";
}
