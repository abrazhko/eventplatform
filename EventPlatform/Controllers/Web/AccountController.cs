using EventPlatform.Data;
using EventPlatform.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatform.Controllers.Web
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _db.Users.FirstOrDefault(x => x.Email == email);

            if (user == null || user.IsBlocked)
            {
                ViewBag.Error = "Неверные учетные данные";
                return View();
            }

            if (user.PasswordHash != password)
            {
                ViewBag.Error = "Неверный пароль";
                return View();
            }

            // Сохраняем пользователя в сессию
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role.ToString());

            //  по роли
            return user.Role switch
            {
                UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                UserRole.Organizer => RedirectToAction("Dashboard", "Organizer"),
                _ => RedirectToAction("Login")
            };
        }

        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var user = _db.Users.FirstOrDefault(u => u.Id == userId.Value);
            if (user == null) return NotFound();

            if (user.Role == UserRole.Admin)
                return RedirectToAction("Profile", "Admin");

            if (user.Role == UserRole.Organizer)
                return RedirectToAction("Profile", "Organizer");

            return NotFound();
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
