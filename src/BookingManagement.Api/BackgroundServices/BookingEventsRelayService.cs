using System.Text.Json;
using BookingManagement.Api.Hubs;
using BookingManagement.Infrastructure.Events;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace BookingManagement.Api.BackgroundServices;

/// <summary>
/// Subscribes to the Redis Pub/Sub channels the booking service publishes to
/// (see RedisBookingEventPublisher) and re-broadcasts each event to the
/// SignalR group for the affected doctor's schedule. This is what makes slot
/// availability update live in every open browser tab without polling.
/// </summary>
public class BookingEventsRelayService : BackgroundService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IHubContext<SchedulingHub> _hubContext;
    private readonly ILogger<BookingEventsRelayService> _logger;

    public BookingEventsRelayService(
        IConnectionMultiplexer connectionMultiplexer,
        IHubContext<SchedulingHub> hubContext,
        ILogger<BookingEventsRelayService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _connectionMultiplexer.GetSubscriber();

        var bookedQueue = await subscriber.SubscribeAsync(RedisChannel.Literal(RedisBookingEventPublisher.AppointmentBookedChannel));
        bookedQueue.OnMessage(message => RelayAsync("SlotBooked", message.Message, stoppingToken));

        var cancelledQueue = await subscriber.SubscribeAsync(RedisChannel.Literal(RedisBookingEventPublisher.AppointmentCancelledChannel));
        cancelledQueue.OnMessage(message => RelayAsync("SlotReleased", message.Message, stoppingToken));

        // Keep the hosted service alive; the ChannelMessageQueue callbacks above
        // fire for the lifetime of the connection.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task RelayAsync(string clientMethod, RedisValue message, CancellationToken cancellationToken)
    {
        try
        {
            using var payload = JsonDocument.Parse(message.ToString());
            if (!payload.RootElement.TryGetProperty("DoctorId", out var doctorIdElement) || doctorIdElement.ValueKind != JsonValueKind.String)
            {
                return;
            }

            var doctorId = doctorIdElement.GetGuid();
            var group = SchedulingHub.DoctorGroupName(doctorId);

            await _hubContext.Clients.Group(group).SendAsync(clientMethod, message.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to relay booking event to SignalR clients");
        }
    }
}
