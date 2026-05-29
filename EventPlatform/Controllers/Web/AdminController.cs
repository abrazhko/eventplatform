using EventPlatform.Data;
using EventPlatform.Models;
using EventPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Controllers.Web
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;


        public AdminController(ApplicationDbContext context,INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> DashboardAsync()
        {
            await CancelPastEventsAsync();
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var timezoneOffset = TimeSpan.FromHours(3);

            // локальное начало месяца
            var nowUtc = DateTime.UtcNow;
            var localNow = nowUtc + timezoneOffset;
            var monthStartLocal = new DateTime(localNow.Year, localNow.Month, 1);

            // перевод в UTC для PostgreSQL
            var monthStartUtc = monthStartLocal - timezoneOffset; 
            monthStartUtc = DateTime.SpecifyKind(monthStartUtc, DateTimeKind.Utc);

            var model = new AdminDashboardViewModel
            {
                Role = "Admin",

                TotalUsers = _context.Users.Count(),

                ActiveOrganizers = _context.Users
                    .Count(u => u.Role == UserRole.Organizer && !u.IsBlocked),

                PublishedEvents = _context.Events
                    .Count(e => e.Status == EventStatus.Published),

                MonthlyRevenue = _context.Tickets
                    .Where(t => t.PurchasedAt >= monthStartUtc)
                    .Sum(t => (decimal?)t.Event.Price) ?? 0,
                
                NewUsersThisWeek = _context.Users
                    .Count(u => u.CreatedAt >= weekAgo),

                EventsToday = _context.Events
                    .Count(e => e.StartDate >= today && e.StartDate < today.AddDays(1)),

                TicketsSoldToday = _context.Tickets
                    .Count(t => t.PurchasedAt >= today && t.PurchasedAt < today.AddDays(1)),

                Last7Days = Enumerable.Range(0, 7)
                    .Select(i => today.AddDays(-i).ToString("dd MMM"))
                    .Reverse()
                    .ToList(),

                TicketsLast7Days = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var day = today.AddDays(-i);
                        var nextDay = day.AddDays(1);

                        return _context.Tickets.Count(t =>
                            t.PurchasedAt >= day && t.PurchasedAt < nextDay);
                    })
                    .Reverse()
                    .ToList(),

                UpcomingEvents = _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Tickets)
                    .Where(e => e.StartDate >= today)
                    .OrderBy(e => e.StartDate)
                    .Take(5)
                    .ToList(),

                LatestUsers = _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        // GET: /Admin/Organizers
        public IActionResult Organizers()
        {
            var organizers = _context.Users
                .Where(u => u.Role == UserRole.Organizer)
                .ToList();

            return View(organizers);
        }

        public IActionResult Profile()
        {
            int userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var user = _context.Users.First(u => u.Id == userId);
            return View(user);
        }


        [HttpGet]
        public IActionResult AddOrganizer() => View();

        [HttpPost]
        public IActionResult AddOrganizer(string fullName, string email, string password)
        {
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = password,
                Role = UserRole.Organizer,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("Organizers");
        }

        [HttpPost]
        public IActionResult BlockOrganizer(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.IsBlocked = !user.IsBlocked;
            _context.SaveChanges();

            return RedirectToAction(nameof(Organizers));
        }
        [HttpPost]
        public IActionResult DeleteOrganizer(int id)
        {
            var user = _context.Users.Find(id);

            if (user == null)
                return NotFound();

            // проверить есть ли мероприятия у организатора
            bool hasEvents = _context.Events.Any(e => e.OrganizerId == id);

            if (hasEvents)
            {
                TempData["Error"] = "Нельзя удалить организатора, так как у него есть мероприятия.";
                return RedirectToAction(nameof(Organizers));
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = "Организатор удалён.";

            return RedirectToAction(nameof(Organizers));
        }


        // GET: /Admin/Events
        //public IActionResult Events()
       // {
         //   var events = _context.Events
              //  .Include(e => e.Organizer)
                //.ToList();

            //return View(events);
        //}

        [HttpPost]
        public IActionResult DeleteEvent(int id)
        {
            var ev = _context.Events.Find(id);
            if (ev == null) return NotFound();

            _context.Events.Remove(ev);
            _context.SaveChanges();

            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public IActionResult AddEvent()
        {
            ViewBag.Organizers = _context.Users.Where(u => u.Role == UserRole.Organizer).ToList();
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddEvent(
            string title,
            string description,
            int organizerId,
            int categoryId,
            DateTime startDate,
            DateTime endDate,
            string location,
            decimal price,
            int capacity,
            IFormFile? image
        )
        {
            if (startDate < DateTime.UtcNow)
            {
                TempData["Error"] ="Нельзя создавать событие в прошлом.";
                ViewBag.Organizers = _context.Users.Where(u => u.Role == UserRole.Organizer).ToList();
                ViewBag.Categories = _context.Categories.ToList();
                return View(); // возвращаем форму с ошибкой
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

            var ev = new Event
            {
                Title = title,
                Description = description,
                OrganizerId = organizerId,
                CategoryId = categoryId,
                StartDate = startDate.ToUniversalTime(),
                EndDate = endDate.ToUniversalTime(),
                Location = location,
                Price = price,
                Capacity = capacity,
                Status = EventStatus.Draft,
                CreatedAt = DateTime.UtcNow,

                ImageUrl = imagePath // сохраняем путь
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return RedirectToAction("Events");
        }
        
        // CATEGORIES
        // GET: /Admin/Categories
        public IActionResult Categories()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        // GET: /Admin/AddCategory
        [HttpGet]
        public IActionResult AddCategory()
        {
            return View();
        }

        // POST: /Admin/AddCategory
        [HttpPost]
        public IActionResult AddCategory(string name)
        {
            var category = new Category
            {
                Name = name
            };

            _context.Categories.Add(category);
            _context.SaveChanges();

            return RedirectToAction("Categories");
        }


        // GET: /Admin/EditCategory/5
        [HttpGet]
        public IActionResult EditCategory(int id)
        {
            var category = _context.Categories.Find(id);

            if (category == null)
                return NotFound();

            return View(category);
        }


        // POST: /Admin/EditCategory
        [HttpPost]
        public IActionResult EditCategory(Category model)
        {
            var category = _context.Categories.Find(model.Id);

            if (category == null)
                return NotFound();

            category.Name = model.Name;

            _context.SaveChanges();

            return RedirectToAction("Categories");
        }


        // POST: /Admin/DeleteCategory
        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);

            if (category == null)
                return NotFound();

            // проверяем используется ли категория
            bool isUsed = _context.Events.Any(e => e.CategoryId == id);

            if (isUsed)
            {
                TempData["Error"] = "Нельзя удалить категорию, так как она используется в мероприятиях.";
                return RedirectToAction("Categories");
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();

            TempData["Success"] = "Категория успешно удалена.";

            return RedirectToAction("Categories");
        }

        // EVENT STATUS
        [HttpPost]
        public async Task<IActionResult> PublishEventAsync(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev.StartDate < DateTime.UtcNow)
            {
                TempData["Error"] = "Нельзя публиковать событие с прошедшей датой.";
                return RedirectToAction(nameof(Events));
            }
            if (ev == null)
                return NotFound();

            ev.Status = EventStatus.Published;

            await _context.SaveChangesAsync();

            // один вызов
            await _notificationService.SendEventPublished(ev.Id);

            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        public async Task<IActionResult> CancelEvent(int id)
        {
            var ev = _context.Events.Find(id);

            if (ev == null)
                return NotFound();

            ev.Status = EventStatus.Cancelled;

            await _context.SaveChangesAsync();

            // уведомление пользователям с билетами
            await _notificationService.SendEventCancelled(ev.Id);

            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        public IActionResult ReturnToDraft(int id)
        {
            var ev = _context.Events.Find(id);
            if (ev == null) return NotFound();

            ev.Status = EventStatus.Draft;
            _context.SaveChanges();

            return RedirectToAction(nameof(Events));
        }
    
        // GLOBAL ANALYTICS 
        public IActionResult Analytics()
        {
            var start = DateTime.UtcNow.AddMonths(-1);

            var model = new AnalyticsViewModel();

            // KPI

            model.TotalUsers = _context.Users.Count();

            model.ActiveUsers = _context.Users
                .Count(u => u.CreatedAt >= start);

            model.TotalEvents = _context.Events.Count();

            model.TotalTickets = _context.Tickets.Count();

            model.TotalRevenue = _context.Tickets
                .Sum(t => (decimal?)t.Event.Price) ?? 0;

            model.AvgTicketPrice = _context.Tickets.Any()
                ? _context.Tickets.Average(t => t.Event.Price)
                : 0;


            // USER GROWTH

            var userStats = _context.Users
                .Where(u => u.CreatedAt >= start)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            model.UserGrowthLabels = userStats
                .Select(x => x.Date.ToString("dd MMM"))
                .ToList();

            model.UserGrowthData = userStats
                .Select(x => x.Count)
                .ToList();


            // TICKET SALES

            var ticketStats = _context.Tickets
                .Where(t => t.PurchasedAt >= start)
                .GroupBy(t => t.PurchasedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            model.TicketSalesLabels = ticketStats
                .Select(x => x.Date.ToString("dd MMM"))
                .ToList();

            model.TicketSalesData = ticketStats
                .Select(x => x.Count)
                .ToList();


            // REVENUE

            var revenueStats = _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid && p.CreatedAt >= start)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            model.RevenueLabels = revenueStats
                .Select(x => x.Date.ToString("dd MMM"))
                .ToList();

            model.RevenueData = revenueStats
                .Select(x => x.Revenue)
                .ToList();


            // CATEGORY POPULARITY

            var categories = _context.Tickets
                .Include(t => t.Event)
                    .ThenInclude(e => e.Category)
                .GroupBy(t => t.Event.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Tickets = g.Count()
                })
                .ToList();

            model.CategoryLabels = categories
                .Select(x => x.Category)
                .ToList();

            model.CategoryData = categories
                .Select(x => x.Tickets)
                .ToList();


            // TOP ORGANIZERS

            model.TopOrganizers = _context.Users
                .Where(u => u.Role == UserRole.Organizer)
                .Select(o => new TopOrganizerViewModel
                {
                    Organizer = o.FullName,

                    Events = _context.Events
                        .Count(e => e.OrganizerId == o.Id),

                    Tickets = _context.Events
                        .Where(e => e.OrganizerId == o.Id)
                        .SelectMany(e => e.Tickets)
                        .Count(),

                    Revenue = _context.Events
                        .Where(e => e.OrganizerId == o.Id)
                        .SelectMany(e => e.Tickets)
                        .Sum(t => (decimal?)t.Event.Price) ?? 0
                })
                .OrderByDescending(o => o.Revenue)
                .Take(5)
                .ToList();


            // TOP EVENTS

            model.TopEvents = _context.Events
                .Include(e => e.Organizer)
                .Select(e => new TopEventViewModel
                {
                    EventTitle = e.Title,
                    Organizer = e.Organizer.FullName,
                    Tickets = e.Tickets.Count(),
                    Revenue = e.Tickets.Sum(t => (decimal?)t.Event.Price) ?? 0
                })
                .OrderByDescending(e => e.Revenue)
                .Take(5)
                .ToList();


            return View(model);
        }

        //ANALYTICS FILTER

        [HttpGet]
        public IActionResult GetAnalytics(int days)
        {
            var start = DateTime.UtcNow.AddDays(-days);

            var tickets = _context.Tickets
                .Include(t => t.Event)
                .Where(t => t.PurchasedAt >= start);

            var data = new
            {
                totalUsers = _context.Users.Count(),

                activeUsers = _context.Users
                    .Count(u => u.CreatedAt >= start),

                totalEvents = _context.Events.Count(),

                totalTickets = tickets.Count(),

                totalRevenue = tickets.Sum(t => (decimal?)t.Event.Price) ?? 0,

                avgTicketPrice = tickets.Any()
                    ? tickets.Average(t => t.Event.Price)
                    : 0,

                ticketLabels = tickets
                    .GroupBy(t => t.PurchasedAt.Date)
                    .Select(g => g.Key.ToString("dd MMM"))
                    .ToList(),

                ticketData = tickets
                    .GroupBy(t => t.PurchasedAt.Date)
                    .Select(g => g.Count())
                    .ToList()
            };

            return Json(data);
        }

        // USERS CHART API
        [HttpGet]
        public IActionResult GetUserStats(int days)
        {
            var start = DateTime.UtcNow.AddDays(-days);

            var stats = _context.Users
                .Where(u => u.CreatedAt >= start)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Json(new
            {
                labels = stats.Select(x => x.Date.ToString("dd MMM")),
                data = stats.Select(x => x.Count)
            });
        }


        // TICKETS CHART API
        [HttpGet]
        public IActionResult GetTicketStats(int days)
        {
            var start = DateTime.UtcNow.AddDays(-days);

            var stats = _context.Tickets
                .Where(t => t.PurchasedAt >= start)
                .GroupBy(t => t.PurchasedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Json(new
            {
                labels = stats.Select(x => x.Date.ToString("dd MMM")),
                data = stats.Select(x => x.Count)
            });
        }


        // REVENUE CHART API
        [HttpGet]
        public IActionResult GetRevenueStats(int days)
        {
            var start = DateTime.UtcNow.AddDays(-days);

            var stats = _context.Tickets
                .Include(t => t.Event)
                .Where(t => t.PurchasedAt >= start)
                .AsEnumerable() // чтобы безопасно использовать DateTime.SpecifyKind
                .Select(t => new
                {
                    Date = DateTime.SpecifyKind(t.PurchasedAt, DateTimeKind.Utc),
                    Revenue = t.Event.Price
                })
                .GroupBy(t => t.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(t => t.Revenue)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Json(new
            {
                labels = stats.Select(x => x.Date.ToString("dd MMM")),
                data = stats.Select(x => x.Revenue)
            });
        }


        // CATEGORY CHART API

        [HttpGet]
        public IActionResult GetCategoryStats(int days)
        {
            var start = DateTime.UtcNow.AddDays(-days);

            var stats = _context.Tickets
                .Include(t => t.Event)
                    .ThenInclude(e => e.Category)
                .Where(t => t.PurchasedAt >= start)
                .GroupBy(t => t.Event.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Tickets = g.Count()
                })
                .ToList();

            return Json(new
            {
                labels = stats.Select(x => x.Category),
                data = stats.Select(x => x.Tickets)
            });
        }

        [HttpGet]
        public IActionResult EditEvent(int id)
        {
            var ev = _context.Events.Find(id);
            if (ev == null) return NotFound();

            ViewBag.Organizers = _context.Users
                .Where(u => u.Role == UserRole.Organizer)
                .ToList();

            ViewBag.Categories = _context.Categories.ToList();

            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> EditEvent(
            int id,
            string title,
            string description,
            int organizerId,
            int categoryId,
            DateTime startDate,
            DateTime endDate,
            string location,
            decimal price,
            int capacity,
            IFormFile? image
        )
        {
            var ev = _context.Events.Find(id);
            if (ev == null) return NotFound();

            if (startDate < DateTime.UtcNow)
            {
                TempData["Error"] = "Дата события не может быть в прошлом.";
                ViewBag.Organizers = _context.Users.Where(u => u.Role == UserRole.Organizer).ToList();
                ViewBag.Categories = _context.Categories.ToList();
                return View(ev);
            }

            // если загрузили новую картинку
            if (image != null && image.Length > 0)
            {
                var folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "events"
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
            ev.OrganizerId = organizerId;
            ev.CategoryId = categoryId;
            ev.StartDate = startDate.ToUniversalTime();
            ev.EndDate = endDate.ToUniversalTime();
            ev.Location = location;
            ev.Price = price;
            ev.Capacity = capacity;

            _context.SaveChanges();

            await _notificationService.SendEventUpdated(ev.Id);

            return RedirectToAction(nameof(Events));
        }
        public IActionResult Events(string? search)
        {
            var query = _context.Events
                .Include(e => e.Organizer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                query = query.Where(e =>
                    e.Title.ToLower().Contains(search) ||
                    e.Location.ToLower().Contains(search) ||
                    e.Organizer.FullName.ToLower().Contains(search)
                );
            }

            var events = query.ToList();

            // проверяем все события на sold out
            foreach (var ev in events)
            {
                _ = CheckAndCancelSoldOutEventAsync(ev); // fire-and-forget, можно await если нужно
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

