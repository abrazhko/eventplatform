using EventPlatform.Data;
using EventPlatform.Models;
using EventPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Controllers.Web
{
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;


        public OrganizerController(ApplicationDbContext context,INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: /Organizer/Dashboard
        public async Task<IActionResult> DashboardAsync()
        {
            int organizerId = GetCurrentUserId();
            await CancelPastEventsAsync();

            var today = DateTime.UtcNow;
            today = DateTime.SpecifyKind(today.Date, DateTimeKind.Utc);
            var weekAgo = today.AddDays(-7);

            var monthStart = DateTime.SpecifyKind(
                new DateTime(today.Year, today.Month, 1), 
                DateTimeKind.Utc
            );

            var events = _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .ToList();

            var model = new OrganizerDashboardViewModel
            {
                Role = "Organizer",
                OrganizerId = organizerId,

                // KPI
                TotalEvents = events.Count,

                PublishedEvents = events.Count(e => e.Status == EventStatus.Published),

                TotalTickets = _context.Tickets
                    .Include(t => t.Event)
                    .Count(t => t.Event.OrganizerId == organizerId),

                MonthlyRevenue = _context.Tickets
                    .Include(t => t.Event)
                    .Where(t => t.Event.OrganizerId == organizerId &&
                                t.PurchasedAt >= monthStart)
                    .Sum(t => (decimal?)t.Event.Price) ?? 0,

                TicketsToday = _context.Tickets
                    .Include(t => t.Event)
                    .Count(t => t.Event.OrganizerId == organizerId &&
                                t.PurchasedAt >= today &&
                                t.PurchasedAt < today.AddDays(1)),

                // ближайшее событие
                NearestEvent = events
                    .Where(e => e.StartDate >= today)
                    .OrderBy(e => e.StartDate)
                    .FirstOrDefault(),

                // график
                Last7Days = Enumerable.Range(0, 7)
                    .Select(i => today.AddDays(-i).ToString("dd MMM"))
                    .Reverse()
                    .ToList(),

                TicketsLast7Days = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var day = today.AddDays(-i);
                        var nextDay = day.AddDays(1);

                        return _context.Tickets
                            .Include(t => t.Event)
                            .Count(t =>
                                t.Event.OrganizerId == organizerId &&
                                t.PurchasedAt >= day &&
                                t.PurchasedAt < nextDay);
                    })
                    .Reverse()
                    .ToList(),

                // ближайшие события
                UpcomingEvents = _context.Events
                    .Include(e => e.Tickets)
                    .Where(e => e.OrganizerId == organizerId && e.StartDate >= today)
                    .OrderBy(e => e.StartDate)
                    .Take(5)
                    .ToList(),

                // последние покупки
                LatestTickets = _context.Tickets
                    .Include(t => t.User)
                    .Include(t => t.Event)
                    .Where(t => t.Event.OrganizerId == organizerId)
                    .OrderByDescending(t => t.PurchasedAt)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Profile()
        {
            int userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = _context.Users.First(u => u.Id == userId);
            return View(user);
        }


        // GET: /Organizer/Events
        //public IActionResult Events()
        //{
          //  int organizerId = GetCurrentUserId();
            //var events = _context.Events
              //  .Where(e => e.OrganizerId == organizerId)
                //.Include(e => e.Category)
                //.Include(e => e.Tickets)
                //.ToList();
            
            //return View(events);
        //}

        // GET: /Organizer/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            string title,
            string description,
            DateTime startDate,
            DateTime endDate,
            string location,
            int categoryId,
            decimal price,
            int capacity,
            IFormFile? image
        )
        {
            if (startDate < DateTime.UtcNow)
            {
                TempData["Error"] = "Нельзя создавать событие в прошлом.";
                ViewBag.Categories = _context.Categories.ToList();
                return View();
            }

            string? imagePath = null;

            if (image != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);

                var path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/events",
                    fileName
                );

                using var stream = new FileStream(path, FileMode.Create);
                await image.CopyToAsync(stream);

                imagePath = "/images/events/" + fileName;
            }

            var newEvent = new Event
            {
                Title = title,
                Description = description,
                OrganizerId = GetCurrentUserId(),
                StartDate = startDate.ToUniversalTime(),
                EndDate = endDate.ToUniversalTime(),
                Location = location,
                CategoryId = categoryId,
                Price = price,
                Capacity = capacity,
                CreatedAt = DateTime.UtcNow,
                Status = EventStatus.Draft,

                ImageUrl = imagePath
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(DashboardAsync));
        }

        // GET: /Organizer/Edit/5
        public IActionResult Edit(int id)
        {
            int organizerId = GetCurrentUserId();
            var evt = _context.Events.FirstOrDefault(e => e.Id == id && e.OrganizerId == organizerId);
            if (evt == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            return View(evt);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            int id,
            string title,
            string description,
            DateTime startDate,
            DateTime endDate,
            string location,
            int categoryId,
            decimal price,
            int capacity,
            IFormFile? image
        )
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
                return NotFound();

            if (startDate < DateTime.UtcNow)
            {
                TempData["Error"] = "Дата события не может быть в прошлом.";
                ViewBag.Categories = _context.Categories.ToList();
                return View(ev);
            }

            if (image != null && image.Length > 0)
            {
                var folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/events"
                );

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName =
                    Guid.NewGuid() +
                    Path.GetExtension(image.FileName);

                var path = Path.Combine(folder, fileName);

                using var stream =
                    new FileStream(path, FileMode.Create);

                await image.CopyToAsync(stream);

                ev.ImageUrl =
                    "/images/events/" + fileName;
            }

            ev.Title = title;
            ev.Description = description;
            ev.StartDate = startDate.ToUniversalTime();
            ev.EndDate = endDate.ToUniversalTime();
            ev.Location = location;
            ev.CategoryId = categoryId;
            ev.Price = price;
            ev.Capacity = capacity;

            await _context.SaveChangesAsync();

            await _notificationService.SendEventUpdated(ev.Id);

            return RedirectToAction("Events");

        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            int organizerId = GetCurrentUserId();
            var evt = _context.Events.FirstOrDefault(e => e.Id == id && e.OrganizerId == organizerId);
            if (evt == null) return NotFound();

            _context.Events.Remove(evt);
            _context.SaveChanges();
            return RedirectToAction(nameof(DashboardAsync));
        }

        // GET: /Organizer/Tickets?eventId=5
        public IActionResult Tickets(int eventId)
        {
            int organizerId = GetCurrentUserId();
            var evt = _context.Events.FirstOrDefault(e => e.Id == eventId && e.OrganizerId == organizerId);
            if (evt == null) return NotFound();

            var tickets = _context.Tickets
                .Include(t => t.User)
                .Where(t => t.EventId == eventId)
                .ToList();

            return View(tickets);
        }

        private int GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
                return userId.Value;

            throw new Exception("Пользователь не авторизован");
        }

        [HttpPost]
        public async Task<IActionResult> PublishAsync(int id)
        {
            int organizerId = GetCurrentUserId();

            var evt = await _context.Events
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == organizerId);

            if (evt.StartDate < DateTime.UtcNow)
            {
                TempData["Error"] = "Нельзя публиковать событие с прошедшей датой.";
                return RedirectToAction(nameof(Events));
            }
            if (evt == null)
                return NotFound();

            evt.Status = EventStatus.Published;

            await _context.SaveChangesAsync();

            await _notificationService.SendEventPublished(evt.Id);

            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            int organizerId = GetCurrentUserId();

            var evt = _context.Events
                .FirstOrDefault(e =>
                    e.Id == id &&
                    e.OrganizerId == organizerId
                );

            if (evt == null)
                return NotFound();

            evt.Status = EventStatus.Cancelled;

            await _context.SaveChangesAsync();

            // отправляем email
            await _notificationService.SendEventCancelled(evt.Id);

            return RedirectToAction(nameof(Events));
        }
        [HttpPost]
        public IActionResult ReturnToDraft(int id)
        {
            int organizerId = GetCurrentUserId();

            var evt = _context.Events.FirstOrDefault(e => e.Id == id && e.OrganizerId == organizerId);
            if (evt == null) return NotFound();

            evt.Status = EventStatus.Draft;
            _context.SaveChanges();

            return RedirectToAction(nameof(Events));
        }

        // GET: /Organizer/Stats/5
        public IActionResult Stats(int id)
        {
            int organizerId = GetCurrentUserId();

            var evt = _context.Events
                .Include(e => e.Tickets)
                .FirstOrDefault(e => e.Id == id && e.OrganizerId == organizerId);

            if (evt == null)
                return NotFound();

            var soldTickets = evt.Tickets.Count;
            var revenue = soldTickets * evt.Price;
            var remaining = evt.Capacity - soldTickets;
            var fillPercent = evt.Capacity == 0
                ? 0
                : (int)((double)soldTickets / evt.Capacity * 100);

            // продажи по дням
            var salesByDay = evt.Tickets
                .GroupBy(t => t.PurchasedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            var model = new EventStatsViewModel
            {
                EventTitle = evt.Title,

                Capacity = evt.Capacity,
                SoldTickets = soldTickets,
                RemainingTickets = remaining,
                Revenue = revenue,
                FillPercent = fillPercent,

                Labels = salesByDay
                    .Select(x => x.Date.ToString("dd MMM"))
                    .ToList(),

                Sales = salesByDay
                    .Select(x => x.Count)
                    .ToList()
            };

            return View(model);
        }
        public IActionResult Events(string? search)
        {
            int organizerId = GetCurrentUserId();

            var query = _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .Include(e => e.Category)
                .Include(e => e.Tickets)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                query = query.Where(e =>
                    e.Title.ToLower().Contains(search) ||
                    e.Location.ToLower().Contains(search)
                );
            }
            var events = query.ToList();

            // проверяем все события на sold out
            foreach (var ev in events)
            {
                _ = CheckAndCancelSoldOutEventAsync(ev); 
            }

            return View(events);
        }
        private async Task CancelPastEventsAsync()
        {
            var now = DateTime.UtcNow;
            var pastEvents = await _context.Events
                .Where(e => e.StartDate < now && e.Status != EventStatus.Cancelled)
                .ToListAsync();

            foreach (var ev in pastEvents)
            {
                ev.Status = EventStatus.Cancelled;
                await _notificationService.SendEventCancelled(ev.Id); 
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckAndCancelSoldOutEventAsync(Event ev)
        {
            if (ev.Capacity > 0 && ev.Tickets.Count >= ev.Capacity)
            {
                if (ev.Status != EventStatus.Cancelled)
                {
                    ev.Status = EventStatus.Cancelled;
                    await _context.SaveChangesAsync();
                    await _notificationService.SendEventCancelled(ev.Id);
                }
            }
        }
    }
}
