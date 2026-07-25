using BookingManagement.NotificationWorker.Models;

namespace BookingManagement.NotificationWorker.Notifications;

public interface INotificationSender
{
    Task SendAppointmentBookedAsync(BookingEventMessage message, CancellationToken cancellationToken = default);

    Task SendAppointmentCancelledAsync(BookingEventMessage message, CancellationToken cancellationToken = default);
}
