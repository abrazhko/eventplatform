using EventPlatform.Data;
using EventPlatform.Models;
using EventPlatform.Models.Dto;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace EventPlatform.Controllers.Api;

[ApiController]
[Route("api/tickets")]
public class TicketsApiController
    : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TicketsApiController(
        ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpPost("buy")]
    public IActionResult BuyTicket(
        [FromBody]
        BuyTicketRequest request)
    {
        int userId = int.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            )
        );

        var ticket = new Ticket
        {
            EventId = request.EventId,

            UserId = userId,

            PurchasedAt =
                DateTime.UtcNow,

            Status =
                TicketStatus.Active,

            QrCode =
                Guid.NewGuid()
                    .ToString()
        };

        _context.Tickets.Add(ticket);

        _context.SaveChanges();

        return Ok(ticket);
    }

    [Authorize]
    [HttpGet("my")]
    public IActionResult MyTickets()
    {
        int userId = int.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            )
        );

        var tickets = _context.Tickets
            .Where(t =>
                t.UserId == userId)
            .Select(t => new
            {
                t.Id,

                t.QrCode,

                t.Status,

                t.PurchasedAt,

                EventTitle =
                    t.Event.Title,

                t.Event.StartDate,

                t.Event.Location
            });

        return Ok(tickets);
    }
}