using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Hubs;
using System.Security.Claims;
using SyncSyntax.Areas.ContentCreator.ViewModels;

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
        [HttpPost]
        public async Task<IActionResult> DeleteMultipleMessagesForMe([FromBody] List<int> ids)
        {
            var currentUserId = _userManager.GetUserId(User);

            var alreadyDeleted = await _context.MessageDeletions
                .Where(d => ids.Contains(d.MessageId) && d.UserId == currentUserId)
                .Select(d => d.MessageId)
                .ToListAsync();

            var toDelete = ids.Except(alreadyDeleted);

            foreach (var id in toDelete)
            {
                _context.MessageDeletions.Add(new MessageDeletion
                {
                    UserId = currentUserId,
                    MessageId = id
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        public IActionResult DeleteMultipleMessagesForAll([FromBody] List<int> ids)
        {
            var currentUserId = _userManager.GetUserId(User);

            var messages = _context.Messages
                .Where(m => ids.Contains(m.Id))
                .ToList();

            foreach (var msg in messages)
            {
                if (msg.SenderId == currentUserId)
                {
                    _context.Messages.Remove(msg);
                }
            }

            _context.SaveChanges();
            return Ok();
        }


        public class PinMessageRequest
        {
            public int MessageId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePinMessage([FromBody] PinMessageRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var message = await _context.Messages.FindAsync(request.MessageId);

                if (message == null) return NotFound("Message not found");
                if (message.SenderId != user.Id && message.ReceiverId != user.Id)
                    return Forbid("You are not allowed");

                if (!message.IsPinned)
                {
                    var existingPinned = await _context.Messages
                       .Where(m => m.IsPinned &&
                              ((m.SenderId == message.SenderId && m.ReceiverId == message.ReceiverId) ||
                               (m.SenderId == message.ReceiverId && m.ReceiverId == message.SenderId)))
                       .ToListAsync();

                    foreach (var pinnedMsg in existingPinned)
                    {
                        pinnedMsg.IsPinned = false;
                    }

                    message.IsPinned = true;
                }
                else
                {
                    message.IsPinned = false;
                }

                _context.Messages.Update(message);
                await _context.SaveChangesAsync();

                if (!message.IsPinned)
                {
                    return Ok(new { success = true, message = (object)null });
                }

                return Ok(new
                {
                    success = true,
                    message = new
                    {
                        id = message.Id,
                        content = message.Content,
                        isPinned = message.IsPinned
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> React([FromBody] ReactionRequestVM request)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages.FindAsync(request.MessageId);
            if (message == null) return NotFound();

            var existing = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == request.MessageId && r.UserId == user.Id);

            if (existing != null)
            {
                existing.Reaction = request.Reaction; // Update reaction
            }
            else
            {
                _context.MessageReactions.Add(new MessageReaction
                {
                    MessageId = request.MessageId,
                    UserId = user.Id,
                    Reaction = request.Reaction
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReaction([FromBody] ReactionRequestVM request)
        {
            var user = await _userManager.GetUserAsync(User);
            var existing = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == request.MessageId && r.UserId == user.Id && r.Reaction == request.Reaction);

            if (existing != null)
            {
                _context.MessageReactions.Remove(existing);
            }
            else
            {
                _context.MessageReactions.Add(new MessageReaction
                {
                    MessageId = request.MessageId,
                    UserId = user.Id,
                    Reaction = request.Reaction
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetReactionsForMessage(int id)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == id)
                .Select(r => new { r.Reaction, r.UserId })
                .ToListAsync();

            return Ok(reactions);
        }


    }
}
