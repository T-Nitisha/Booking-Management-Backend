namespace BookingManagement.Infrastructure.Messaging;

/// <summary>
/// Confluent Cloud connection settings, bound from the "Kafka" configuration
/// section. When <see cref="Enabled"/> is false (the default for local dev,
/// where no Confluent Cloud cluster is configured), Kafka publishing is a
/// no-op so the rest of the app runs without requiring live Kafka credentials.
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = string.Empty;
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;
    public string AppointmentBookedTopic { get; set; } = "appointment-booked";
    public string AppointmentCancelledTopic { get; set; } = "appointment-cancelled";
}
