using BookingManagement.NotificationWorker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BookingManagement.NotificationWorker.Notifications;

public class SendGridNotificationSender : INotificationSender
{
    private readonly ISendGridClient _client;
    private readonly NotificationOptions _options;
    private readonly ILogger<SendGridNotificationSender> _logger;

    public SendGridNotificationSender(ISendGridClient client, IOptions<NotificationOptions> options, ILogger<SendGridNotificationSender> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public Task SendAppointmentBookedAsync(BookingEventMessage message, CancellationToken cancellationToken = default)
        => SendAsync(
            message,
            "Your appointment is confirmed",
            $"Your appointment with Dr. {message.DoctorName} on {message.StartTimeUtc:f} is confirmed.",
            cancellationToken);

    public Task SendAppointmentCancelledAsync(BookingEventMessage message, CancellationToken cancellationToken = default)
        => SendAsync(
            message,
            "Your appointment was cancelled",
            $"Your appointment with Dr. {message.DoctorName} on {message.StartTimeUtc:f} has been cancelled.",
            cancellationToken);

    private async Task SendAsync(BookingEventMessage message, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.PatientEmail))
        {
            _logger.LogWarning("Skipping email for appointment {AppointmentId}: no patient email on the event.", message.Id);
            return;
        }

        var email = MailHelper.CreateSingleEmail(
            new EmailAddress(_options.FromEmail, _options.FromName),
            new EmailAddress(message.PatientEmail, message.PatientName),
            subject,
            body,
            $"<p>{body}</p>");

        var response = await _client.SendEmailAsync(email, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SendGrid returned {StatusCode} for appointment {AppointmentId}", response.StatusCode, message.Id);
        }
    }
}
