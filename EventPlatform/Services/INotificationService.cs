namespace EventPlatform.Services;

public interface INotificationService
{
    Task SendEventCancelled(int eventId);

    Task SendEventUpdated(int eventId);
    Task SendEventPublished(int eventId);

    Task SendReminderNotifications();
}