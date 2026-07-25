using System.Text.Json;
using BookingManagement.Domain.Entities;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingManagement.Infrastructure.Messaging;

/// <summary>
/// Publishes booking events to Confluent Cloud Kafka. Unlike the Redis Pub/Sub
/// publisher (fire-and-forget, used purely to drive the live UI), Kafka here is
/// the durable backbone: messages are retained and replayable, so the
/// Notification worker and any future audit/reporting consumer can read them
/// even if they were offline when the event was produced.
/// </summary>
public class KafkaBookingEventPublisher : IKafkaBookingEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaBookingEventPublisher> _logger;

    public KafkaBookingEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaBookingEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = _options.SaslUsername,
            SaslPassword = _options.SaslPassword,
            // Guarantees the broker acknowledges the write before ProduceAsync
            // completes, matching the "durable audit trail" purpose of this topic.
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => PublishAsync(_options.AppointmentBookedTopic, appointment);

    public Task PublishAppointmentCancelledAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => PublishAsync(_options.AppointmentCancelledTopic, appointment);

    private async Task PublishAsync(string topic, Appointment appointment)
    {
        var value = JsonSerializer.Serialize(new
        {
            appointment.Id,
            appointment.SlotId,
            appointment.PatientId,
            Status = appointment.Status.ToString(),
            DoctorId = appointment.Slot?.DoctorId,
            DoctorName = appointment.Slot?.Doctor?.FullName,
            StartTimeUtc = appointment.Slot?.StartTimeUtc,
            PatientEmail = appointment.Patient?.Email,
            PatientName = appointment.Patient?.FullName,
            OccurredAtUtc = DateTime.UtcNow
        });

        try
        {
            await _producer.ProduceAsync(topic, new Message<string, string> { Key = appointment.Id.ToString(), Value = value });
        }
        catch (ProduceException<string, string> ex)
        {
            // Kafka publish failure should not fail the booking itself (Postgres is
            // the source of truth and Redis already notified live viewers) —
            // log so the gap is visible without blocking the user-facing request.
            _logger.LogError(ex, "Failed to publish {Topic} event for appointment {AppointmentId}", topic, appointment.Id);
        }
    }

    public void Dispose() => _producer.Dispose();
}
