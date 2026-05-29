using EventPlatform.Data;
using EventPlatform.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventPlatform.Controllers.Api
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetProfile()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Users.Find(userId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.FullName,
                user.Email
            });
        }

        // Метод для редактирования профиля доклепать потом 
        [Authorize]
        [HttpPut]
        public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Users.Find(userId);

            if (user == null) return NotFound();

            // Обновляем данные
            user.FullName = request.FullName;
            user.Email = request.Email;

            _context.SaveChanges();

            return Ok(new
            {
                user.FullName,
                user.Email
            });
        }
    }

}