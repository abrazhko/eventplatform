using Microsoft.EntityFrameworkCore;
using EventPlatform.Data;
using EventPlatform.Models;

namespace EventPlatform.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    private readonly IEmailService _emailService;

    public NotificationService(
        ApplicationDbContext context,
        IEmailService emailService
    )
    {
        _context = context;
        _emailService = emailService;
    }

    private async Task<List<User>> GetUsersWithTickets(int eventId)
    {
        return await _context.Tickets
            .Where(t => t.EventId == eventId)
            .Select(t => t.User)
            .Distinct()
            .ToListAsync();
    }

    public async Task SendEventCancelled(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return;

        // Берем все билеты события с пользователями
        var tickets = await _context.Tickets
            .Where(t => t.EventId == eventId)
            .Include(t => t.User)
            .ToListAsync();

        // Меняем статус всех билетов на отменён
        tickets.ForEach(t => t.Status = TicketStatus.Cancelled);

        // Группируем билеты по пользователю, чтобы отправить письмо один раз
        var users = tickets
            .Select(t => t.User)
            .Distinct()
            .ToList();

        foreach (var user in users)
        {
            var notification = new Notification
            {
                UserId = user.Id,
                EventId = eventId,
                Title = "Мероприятие отменено",
                Message = $"Событие \"{ev.Title}\" было отменено",
                Type = "Cancellation"
            };

            _context.Notifications.Add(notification);

            await _emailService.SendEmailAsync(
                user.Email,
                notification.Title,
                $"""
                <h2>{notification.Title}</h2>
                <p>{notification.Message}</p>
                <p>Дата: {ev.StartDate.ToLocalTime():dd.MM.yyyy HH:mm}</p>
                <p>Место: {ev.Location}</p>
                """
            );

            notification.EmailSent = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SendEventUpdated(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);

        var users = await GetUsersWithTickets(eventId);

        foreach (var user in users)
        {
            var notification = new Notification
            {
                UserId = user.Id,
                EventId = eventId,
                Title = "Изменение мероприятия",
                Message = $"Изменилось время или место события \"{ev.Title}\"",
                Type = "ScheduleChange"
            };

            _context.Notifications.Add(notification);

            await _emailService.SendEmailAsync(
                user.Email,
                notification.Title,
                $"""
                <h2>{notification.Title}</h2>

                <p>{notification.Message}</p>

                <p>Дата: {ev.StartDate.ToLocalTime():dd.MM.yyyy HH:mm}</p>
                <p>Место: {ev.Location}</p>
                """
            );

            notification.EmailSent = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SendReminderNotifications()
    {
        // Получаем текущее время в локальной временной зоне
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);

        // Определяем диапазон для напоминаний
        var reminderStart = nowLocal.AddHours(24).AddMinutes(-5); 
        var reminderEnd = nowLocal.AddHours(24).AddMinutes(5);

        // Преобразуем локальное время в UTC для бдшки
        var reminderStartUtc = TimeZoneInfo.ConvertTimeToUtc(reminderStart);
        var reminderEndUtc = TimeZoneInfo.ConvertTimeToUtc(reminderEnd);

        Console.WriteLine("REMINDER RANGE:");
        Console.WriteLine($"Now: {nowLocal:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Start UTC: {reminderStartUtc:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"End UTC:   {reminderEndUtc:yyyy-MM-dd HH:mm:ss}");

        // Получаем события в диапазоне
        var events = await _context.Events
            .Where(e =>
                e.StartDate >= reminderStartUtc &&  
                e.StartDate <= reminderEndUtc &&  
                e.Status == EventStatus.Published
            )
            .ToListAsync();

        Console.WriteLine($"Events found: {events.Count}");

        foreach (var ev in events)
        {
            var users = await _context.Tickets
                .Where(t =>
                    t.EventId == ev.Id &&
                    t.Status == TicketStatus.Active
                )
                .Select(t => t.User)
                .Distinct()
                .ToListAsync();

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    EventId = ev.Id,
                    Title = "Напоминание о мероприятии",
                    Message = $"Событие \"{ev.Title}\" начнётся через 24 часа",
                    Type = "Reminder",
                    CreatedAt = DateTime.UtcNow  
                };

                _context.Notifications.Add(notification);

                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        notification.Title,
                        $"""
                        <h2>{notification.Title}</h2>

                        <p>{notification.Message}</p>

                        <p>Дата: {ev.StartDate.ToLocalTime():dd.MM.yyyy HH:mm}</p>
                        <p>Место: {ev.Location}</p>
                        """
                    );

                    notification.EmailSent = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email to {user.Email}: {ex.Message}");
                    notification.EmailSent = false; // Устанавливаем в false, если не получилось отправить email
                }
            }
        }

        await _context.SaveChangesAsync();

        Console.WriteLine("Reminder check finished");
    }

    public async Task SendEventPublished(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return;

        var tickets = await _context.Tickets
            .Where(t => t.EventId == eventId)
            .Include(t => t.User)
            .ToListAsync();

        if (!tickets.Any())
            return;

        // меняем статус билетов обратно на активный
        foreach (var ticket in tickets)
            ticket.Status = TicketStatus.Active;

        var users = tickets
            .Where(t => t.User != null)
            .Select(t => t.User)
            .Distinct()
            .ToList();

        foreach (var user in users)
        {
            var notification = new Notification
            {
                UserId = user.Id,
                EventId = eventId,
                Title = "Мероприятие снова доступно",
                Message = $"Событие \"{ev.Title}\" снова опубликовано",
                Type = "Published",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);

            await _emailService.SendEmailAsync(
                user.Email,
                notification.Title,
                $"""
                <h2>{notification.Title}</h2>

                <p>{notification.Message}</p>

                <p>Дата: {ev.StartDate.ToLocalTime():dd.MM.yyyy HH:mm}</p>
                <p>Место: {ev.Location}</p>
                """
            );

            notification.EmailSent = true;
        }

        await _context.SaveChangesAsync();
    }
}