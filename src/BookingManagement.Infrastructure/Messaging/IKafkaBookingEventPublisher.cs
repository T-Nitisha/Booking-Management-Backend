using BookingManagement.Application.Abstractions;

namespace BookingManagement.Infrastructure.Messaging;

/// <summary>
/// Marker interface distinguishing the Kafka-backed publisher from the
/// Redis-backed one, so both can be composed by <see cref="CompositeBookingEventPublisher"/>
/// without ambiguity when resolved from DI.
/// </summary>
public interface IKafkaBookingEventPublisher : IBookingEventPublisher
{
}
