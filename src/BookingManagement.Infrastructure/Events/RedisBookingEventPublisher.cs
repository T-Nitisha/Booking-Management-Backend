using System.Text.Json;
using BookingManagement.Application.Abstractions;
using BookingManagement.Domain.Entities;
using StackExchange.Redis;

namespace BookingManagement.Infrastructure.Events;

/// <summary>
/// Publishes booking events over Redis Pub/Sub. The SignalR hub subscribes to
/// these channels and re-broadcasts to connected clients, so anyone viewing a
/// doctor's schedule sees a slot flip to "Booked" in real time without a
/// heavier message broker (see architecture discussion: Kafka is reserved for
/// anything needing guaranteed delivery/replay, e.g. audit trails).
/// </summary>
public class RedisBookingEventPublisher : IBookingEventPublisher
{
    public const string AppointmentBookedChannel = "appointments:booked";
    public const string AppointmentCancelledChannel = "appointments:cancelled";

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisBookingEventPublisher(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => PublishAsync(AppointmentBookedChannel, appointment);

    public Task PublishAppointmentCancelledAsync(Appointment appointment, CancellationToken cancellationToken = default)
        => PublishAsync(AppointmentCancelledChannel, appointment);

    private async Task PublishAsync(string channel, Appointment appointment)
    {
        var subscriber = _connectionMultiplexer.GetSubscriber();
        var payload = JsonSerializer.Serialize(new
        {
            appointment.Id,
            appointment.SlotId,
            appointment.PatientId,
            Status = appointment.Status.ToString(),
            DoctorId = appointment.Slot?.DoctorId,
            StartTimeUtc = appointment.Slot?.StartTimeUtc
        });

        await subscriber.PublishAsync(RedisChannel.Literal(channel), payload);
    }
}
