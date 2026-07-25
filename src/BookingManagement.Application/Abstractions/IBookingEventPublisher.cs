using BookingManagement.Domain.Entities;

namespace BookingManagement.Application.Abstractions;

/// <summary>
/// Publishes domain events that other parts of the system react to
/// (SignalR broadcast to viewers of the slot, Kafka/notification consumers, etc.).
/// Implemented in Infrastructure; kept abstract here so Application stays
/// free of any specific messaging technology.
/// </summary>
public interface IBookingEventPublisher
{
    Task PublishAppointmentBookedAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task PublishAppointmentCancelledAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
