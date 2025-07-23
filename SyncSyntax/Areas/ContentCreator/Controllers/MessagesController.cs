using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Hubs;
using System.Security.Claims;

namespace SyncSyntax.Areas.ContentCreator.Controllers
{
    [Authorize(Roles = "ContentCreator, Admin")]
    [Area("ContentCreator")]
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<PostController> _logger;
        public MessagesController(AppDbContext context, UserManager<AppUser> userManager, IHubContext<ChatHub> hubContext, ILogger<PostController> logger)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var conversations = await _context.Messages
                .Where(m => (m.SenderId == user.Id || m.ReceiverId == user.Id)
                            && !_context.MessageDeletions.Any(d => d.UserId == user.Id && d.MessageId == m.Id))
                .GroupBy(m => m.SenderId == user.Id ? m.ReceiverId : m.SenderId)
                .Select(g => g.OrderByDescending(m => m.SentAt).FirstOrDefault())
                .ToListAsync();

            var availableUsers = await _context.Users
                .Where(u => u.Id != user.Id)
                .ToListAsync();

            if (conversations == null || !conversations.Any())
            {
                ViewData["Message"] = "That not found any chats yet!!";
            }

            var otherUserIds = conversations
                .Select(c => c.SenderId == user.Id ? c.ReceiverId : c.SenderId)
                .Distinct()
                .ToList();

            var otherUsers = await _context.Users
                .Where(u => otherUserIds.Contains(u.Id))
                .ToListAsync();

            var model = new ChatViewModel
            {
                CurrentUserId = user.Id,
                Conversations = conversations,
                OtherUsers = otherUsers,
                AvailableUsers = availableUsers
            };

            return View(model);
        }

        public async Task<IActionResult> Chat(string userId)
        {
            var user = await _userManager.GetUserAsync(User);

            // تحديث آخر ظهور للمستخدم الحالي
            user.LastSeen = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // تحديث الرسائل غير المقروءة كما عندك...
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == userId && m.ReceiverId == user.Id && !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            if (unreadMessages.Any())
                await _context.SaveChangesAsync();

            var chatMessages = await _context.Messages
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == userId) ||
                            (m.ReceiverId == user.Id && m.SenderId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var chatUser = await _context.Users.FindAsync(userId);

            ViewBag.ChatUser = chatUser;

            return View(chatMessages);
        }

 
        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id, string scope)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var message = await _context.Messages.FindAsync(id);

            if (message == null || currentUserId == null)
                return NotFound();

            if (scope == "all")
            {
                if (message.SenderId == currentUserId)
                {
                    _context.Messages.Remove(message);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
                else
                {
                    return Forbid(); // المستقبل لا يمكنه حذف الرسالة للجميع
                }
            }
            else if (scope == "me")
            {
                // تحقق إذا تم حذفها مسبقًا
                var alreadyDeleted = await _context.MessageDeletions
                    .AnyAsync(d => d.UserId == currentUserId && d.MessageId == message.Id);

                if (!alreadyDeleted)
                {
                    var deletion = new MessageDeletion
                    {
                        UserId = currentUserId,
                        MessageId = message.Id
                    };

                    _context.MessageDeletions.Add(deletion);
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(int id, string content)
        {
            var currentUserId = _userManager.GetUserId(User);
            var message = await _context.Messages.FindAsync(id);

            if (message == null || message.SenderId != currentUserId)
                return Forbid();   

            message.Content = content;
            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpGet]
        public IActionResult MessageInfo(int id)
        {
            var message = _context.Messages.FirstOrDefault(m => m.Id == id);
            var currentUserId = _userManager.GetUserId(User);

            if (message == null || message.SenderId != currentUserId)
                return Forbid();  

            return Json(new
            {
                sentAt = message.SentAt.ToLocalTime().ToString("f"),
                isRead = message.IsRead,
                readAt = message.ReadAt?.ToLocalTime().ToString("f")
            });
        }


        public async Task MarkMessagesAsRead(string userId, string fromUserId)
        {
            var unreadMessages = _context.Messages
                .Where(m => m.ReceiverId == userId && m.SenderId == fromUserId && !m.IsRead);

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.UtcNow;  
            }

            await _context.SaveChangesAsync();
        }


    }
}
