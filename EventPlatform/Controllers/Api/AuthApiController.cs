using EventPlatform.Data;
using EventPlatform.Models.Dto;
using Microsoft.AspNetCore.Mvc;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EventPlatform.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public IActionResult Login(
        [FromBody] LoginRequest request
    )
    {
        var user = _context.Users
            .FirstOrDefault(u =>
                u.Email == request.Email &&
                !u.IsBlocked
            );

        if (user == null)
            return Unauthorized();

        if (user.PasswordHash != request.Password)
            return Unauthorized();

        var key = Encoding.UTF8.GetBytes(
            "THIS_IS_SUPER_SECRET_KEY_123456789012345"
        );

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Email,
                user.Email
            )
        };

        var token =
            new JwtSecurityToken(
                claims: claims,
                expires:
                    DateTime.UtcNow.AddDays(7),
                signingCredentials:
                    new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256
                    )
            );

        return Ok(new
        {
            token =
                new JwtSecurityTokenHandler()
                    .WriteToken(token),

            user.Id,
            user.FullName,
            user.Email
        });
    }

    [HttpPost("register")]
    public IActionResult Register(
        [FromBody] RegisterRequest request
    )
    {
        if (_context.Users.Any(u =>
            u.Email == request.Email))
        {
            return BadRequest("Email exists");
        }

        var user = new Models.User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = request.Password,
            Role = Models.UserRole.Participant,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        _context.SaveChanges();

        return Ok();
    }
}