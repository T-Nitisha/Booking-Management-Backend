using BookingManagement.NotificationWorker.Models;
using Microsoft.Extensions.Logging;

namespace BookingManagement.NotificationWorker.Notifications;

/// <summary>Logs the notification instead of sending a real email. Default for local dev/demo without a SendGrid key.</summary>
public class ConsoleNotificationSender : INotificationSender
{
    private readonly ILogger<ConsoleNotificationSender> _logger;

    public ConsoleNotificationSender(ILogger<ConsoleNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAppointmentBookedAsync(BookingEventMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Notification] Would email {PatientEmail}: your appointment with Dr. {DoctorName} on {StartTimeUtc:u} is confirmed.",
            message.PatientEmail, message.DoctorName, message.StartTimeUtc);
        return Task.CompletedTask;
    }

    public Task SendAppointmentCancelledAsync(BookingEventMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Notification] Would email {PatientEmail}: your appointment with Dr. {DoctorName} on {StartTimeUtc:u} was cancelled.",
            message.PatientEmail, message.DoctorName, message.StartTimeUtc);
        return Task.CompletedTask;
    }
}
