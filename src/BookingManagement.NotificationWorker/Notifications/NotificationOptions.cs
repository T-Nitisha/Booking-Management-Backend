namespace BookingManagement.NotificationWorker.Notifications;

public class NotificationOptions
{
    public const string SectionName = "Notification";

    /// <summary>"SendGrid" or "Console" (Console just logs — useful for local dev / demos without an API key).</summary>
    public string Provider { get; set; } = "Console";
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@booking-management.example.com";
    public string FromName { get; set; } = "Booking Management";
}
