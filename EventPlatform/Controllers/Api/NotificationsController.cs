using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventPlatform.Data;
using EventPlatform.Models.Dto;

namespace EventPlatform.Controllers.Api;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(
        ApplicationDbContext context
    )
    {
        _context = context;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        int userId =
            int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )
            );

        var data =
            await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n =>
                    new NotificationDto
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        EventTitle =
                            n.Event != null
                                ? n.Event.Title
                                : null,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead
                    }
                )
                .ToListAsync();

        return Ok(data);
    }

    [HttpPost("mark-read/{id}")]
    public async Task<IActionResult> MarkRead(
        int id
    )
    {
        int userId =
            int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )
            );

        var notif =
            await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id
                    && n.UserId == userId
                );

        if (notif == null)
            return NotFound();

        notif.IsRead = true;

        await _context.SaveChangesAsync();

        return Ok();
    }
}