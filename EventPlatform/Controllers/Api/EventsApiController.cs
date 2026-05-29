using EventPlatform.Data;
using EventPlatform.Models;
using EventPlatform.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatform.Controllers.Api;

[ApiController]
[Route("api/events")]
public class EventsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EventsApiController(
        ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetEvents(
        string? search,
        string? city,
        decimal? maxPrice
    )
    {
        var query = _context.Events
            .Include(e => e.Category)
            .Include(e => e.Tickets)
            .Where(e => e.Status == EventStatus.Published);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(e => e.Title.Contains(search));
        }

        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(e => e.Location.Contains(city));
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(e => e.Price <= maxPrice);
        }

        var events = query.Select(e => new
        {
            e.Id,
            e.Title,
            e.Description,
            e.StartDate,
            e.Location,
            e.Price,
            Category = e.Category.Name,

            TicketsLeft = e.Capacity - e.Tickets.Count(),

            
            ImageUrl = e.ImageUrl   
            
        });

        return Ok(events);
    }

    [HttpGet("{id}")]
    public IActionResult GetEvent(int id)
    {
        var ev = _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .FirstOrDefault(e => e.Id == id);

        if (ev == null)
            return NotFound();

        return Ok(new
        {
            ev.Id,
            ev.Title,
            ev.Description,
            ev.StartDate,
            ev.EndDate,
            ev.Location,
            ev.Price,
            Category = ev.Category.Name,
            Organizer = ev.Organizer.FullName,
            TicketsLeft = ev.Capacity - ev.Tickets.Count(),

            
            ImageUrl = ev.ImageUrl    
            
        });
    }
    [HttpPost("filter")]
    public IActionResult FilterEvents([FromBody] FilterRequest request)
    {
        var query = _context.Events
            .Include(e => e.Category)
            .Include(e => e.Tickets)
            .Where(e => e.Status == EventStatus.Published);

        // Поиск по названию
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(e => e.Title.Contains(request.Search));

        // Фильтр по категории
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(e => e.Category.Name == request.Category);

        // Фильтр по цене
        if (request.PriceMin.HasValue)
            query = query.Where(e => e.Price >= request.PriceMin.Value);
        if (request.PriceMax.HasValue)
            query = query.Where(e => e.Price <= request.PriceMax.Value);

        // Фильтр бесплатных
        if (request.OnlyFree.HasValue && request.OnlyFree.Value)
            query = query.Where(e => e.Price == 0);

        // Фильтр по дате
        if (!string.IsNullOrWhiteSpace(request.DateFrom) &&
            DateTime.TryParse(request.DateFrom, out var from))
        {
            var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            query = query.Where(e => e.StartDate >= fromUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.DateTo) &&
            DateTime.TryParse(request.DateTo, out var to))
        {
            var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc).AddDays(1);
            query = query.Where(e => e.StartDate < toUtc);
        }

        var events = query.Select(e => new
        {
            e.Id,
            e.Title,
            e.Description,
            e.StartDate,
            e.EndDate,
            e.Location,
            e.Price,
            Category = e.Category.Name,
            TicketsLeft = e.Capacity - e.Tickets.Count(),
            ImageUrl = e.ImageUrl
        }).ToList();

        return Ok(events);
    }
}