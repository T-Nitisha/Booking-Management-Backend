using BookingManagement.Application.Abstractions;
using BookingManagement.Domain.Entities;
using BookingManagement.Infrastructure.Events;

namespace BookingManagement.Infrastructure.Messaging;

/// <summary>
/// Fans each booking event out to both channels: Redis Pub/Sub (fast,
/// fire-and-forget, drives the live SignalR UI) and Kafka (durable, replayable,
/// consumed by the Notification/Waitlist background workers). Registered as the
/// single <see cref="IBookingEventPublisher"/> so <c>AppointmentBookingService</c>
/// doesn't need to know both exist.
/// </summary>
public class CompositeBookingEventPublisher : IBookingEventPublisher
{
    private readonly RedisBookingEventPublisher _redisPublisher;
    private readonly IKafkaBookingEventPublisher _kafkaPublisher;

    public CompositeBookingEventPublisher(RedisBookingEventPublisher redisPublisher, IKafkaBookingEventPublisher kafkaPublisher)
    {
        _redisPublisher = redisPublisher;
        _kafkaPublisher = kafkaPublisher;
    }

    public Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => Task.WhenAll(
            _redisPublisher.PublishAppointmentBookedAsync(appointment, cancellationToken),
            _kafkaPublisher.PublishAppointmentBookedAsync(appointment, cancellationToken));

    public Task PublishAppointmentCancelledAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => Task.WhenAll(
            _redisPublisher.PublishAppointmentCancelledAsync(appointment, cancellationToken),
            _kafkaPublisher.PublishAppointmentCancelledAsync(appointment, cancellationToken));
}
