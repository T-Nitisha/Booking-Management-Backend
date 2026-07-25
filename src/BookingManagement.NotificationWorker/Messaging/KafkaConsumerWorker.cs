using System.Text.Json;
using BookingManagement.NotificationWorker.Models;
using BookingManagement.NotificationWorker.Notifications;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace BookingManagement.NotificationWorker.Messaging;

/// <summary>
/// Consumes the durable appointment-booked/appointment-cancelled topics produced
/// by the Api and dispatches to <see cref="INotificationSender"/>. Runs as its
/// own long-lived background service (Confluent.Kafka's IConsumer.Consume is
/// blocking, so it runs on a dedicated thread rather than an async loop).
/// </summary>
public class KafkaConsumerWorker : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerWorker> _logger;

    public KafkaConsumerWorker(IOptions<KafkaOptions> options, IServiceScopeFactory scopeFactory, ILogger<KafkaConsumerWorker> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Kafka disabled (Kafka:Enabled=false); notification worker is idle.");
            return Task.CompletedTask;
        }

        return Task.Factory.StartNew(() => ConsumeLoop(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = _options.SaslUsername,
            SaslPassword = _options.SaslPassword,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { _options.AppointmentBookedTopic, _options.AppointmentCancelledTopic });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                    continue;
                }

                if (result?.Message is null)
                {
                    continue;
                }

                DispatchAsync(result.Topic, result.Message.Value, stoppingToken).GetAwaiter().GetResult();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task DispatchAsync(string topic, string json, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<BookingEventMessage>(json);
            if (message is null)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

            if (topic == _options.AppointmentBookedTopic)
            {
                await sender.SendAppointmentBookedAsync(message, cancellationToken);
            }
            else if (topic == _options.AppointmentCancelledTopic)
            {
                await sender.SendAppointmentCancelledAsync(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message from topic {Topic}", topic);
        }
    }
}
