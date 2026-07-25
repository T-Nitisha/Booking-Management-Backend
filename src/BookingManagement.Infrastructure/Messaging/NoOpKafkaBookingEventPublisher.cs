using BookingManagement.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BookingManagement.Infrastructure.Messaging;

/// <summary>Used when Kafka:Enabled is false (e.g. local dev without a Confluent Cloud cluster).</summary>
public class NoOpKafkaBookingEventPublisher : IKafkaBookingEventPublisher
{
    private readonly ILogger<NoOpKafkaBookingEventPublisher> _logger;

    public NoOpKafkaBookingEventPublisher(ILogger<NoOpKafkaBookingEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Kafka disabled; skipping AppointmentBooked publish for {AppointmentId}", appointment.Id);
        return Task.CompletedTask;
    }

    public Task PublishAppointmentCancelledAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Kafka disabled; skipping AppointmentCancelled publish for {AppointmentId}", appointment.Id);
        return Task.CompletedTask;
    }
}
