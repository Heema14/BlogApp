using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SyncSyntax.Data;
using SyncSyntax.Models;
using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SyncSyntax.Controllers
{
    [Authorize(Roles = "ContentCreator, Admin")]
    [Area("ContentCreator")]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public NotificationController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.User) 
                .ToListAsync();
            var unreadNotificationsCount = _context.Notifications
                       .Where(n => n.UserId == userId && !n.IsRead)
                       .Count();

            ViewBag.UnreadNotificationsCount = unreadNotificationsCount;
            var currentUserId = _userManager.GetUserId(User);
            ViewBag.UnreadCount = _context.Messages
                .Count(m => m.ReceiverId == currentUserId && !m.IsRead);

            return View(notifications);
        }


        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
