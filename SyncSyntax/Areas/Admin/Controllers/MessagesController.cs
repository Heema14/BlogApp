using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using System.Security.Claims;
using SyncSyntax.Areas.ContentCreator.ViewModels;
using SyncSyntax.Models.Hubs;

namespace SyncSyntax.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessagesController> _logger;
        public MessagesController(AppDbContext context, UserManager<AppUser> userManager, IHubContext<ChatHub> hubContext, ILogger<MessagesController> logger)
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

            user.LastSeen = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

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

            var pinnedMessage = await _context.Messages
               .Where(m => ((m.SenderId == user.Id && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == user.Id)) &&
                            m.IsPinned == true)
               .FirstOrDefaultAsync();

            var chatUser = await _context.Users.FindAsync(userId);

            var messageIds = chatMessages.Select(m => m.Id).ToList();

            var reactions = await _context.MessageReactions
     .Where(r => messageIds.Contains(r.MessageId))
     .Include(r => r.User)
     .ToListAsync();

            var groupedReactions = reactions
                .GroupBy(r => r.MessageId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(r => r.Reaction).Select(rg => new
                    {
                        Emoji = rg.Key,
                        Count = rg.Count(),
                        Users = rg.Select(u => u.User.FirstName).ToList()
                    }).ToList()
                );

            ViewBag.MessageReactions = groupedReactions;

            ViewBag.ChatUser = chatUser;
            ViewBag.PinnedMessage = pinnedMessage;

            return View(chatMessages);
        }


 
 

    }
}
