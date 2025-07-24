using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using SyncSyntax.Data;
using SyncSyntax.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SyncSyntax.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ChatHub(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            var senderUser = await _userManager.FindByIdAsync(senderId);
            if (senderUser == null) return;

            var newMessage = new Message
            {
                SenderId = senderUser.Id,
                ReceiverId = receiverId,
                Content = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            // أضف المستخدمين لمجموعة الرسالة الجديدة
            await Groups.AddToGroupAsync(Context.ConnectionId, newMessage.Id.ToString()); // للمستخدم الحالي (المرسل)

            // ملاحظة: المستخدم المستقبل قد لا يكون متصل بنفس الاتصال، لذلك نرسل له رسالة مباشرة
            // أو يمكن تعيين منطق إضافي في OnConnectedAsync ليضيف المستخدم تلقائيًا لكل مجموعات الرسائل الخاصة به

            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == newMessage.Id)
                .GroupBy(r => r.Reaction)
                .Select(g => new { Reaction = g.Key, Count = g.Count() })
                .ToListAsync();

            // إرسال للمستقبل والمُرسل
            await Clients.User(receiverId).SendAsync("ReceiveMessage",
                senderUser.Id,
                message,
                newMessage.Id,
                newMessage.SentAt,
                newMessage.IsRead,
                newMessage.IsPinned,
                reactions);

            await Clients.User(senderUser.Id).SendAsync("ReceiveMessage",
                senderUser.Id,
                message,
                newMessage.Id,
                newMessage.SentAt,
                newMessage.IsRead,
                newMessage.IsPinned,
                reactions);
        }
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // تأكد أن الـ UserIdentifier معرف في إعدادات SignalR

            // جلب كل رسائل المستخدم (مرسلة أو مستقبلة)
            var messageIds = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.Id.ToString())
                .ToListAsync();

            foreach (var msgId in messageIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, msgId);
            }

            await base.OnConnectedAsync();
        }


        public async Task SendReaction(string userId, int messageId, string reaction)
        {
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MessageId == messageId);

            if (existingReaction == null)
            {
                // إضافة تفاعل جديد
                var newReaction = new MessageReaction
                {
                    UserId = userId,
                    MessageId = messageId,
                    Reaction = reaction,
                    ReactedAt = DateTime.UtcNow
                };
                _context.MessageReactions.Add(newReaction);
            }
            else
            {
                if (existingReaction.Reaction == reaction)
                {
                    // حذف التفاعل (toggle off)
                    _context.MessageReactions.Remove(existingReaction);
                }
                else
                {
                    // تغيير التفاعل
                    existingReaction.Reaction = reaction;
                    existingReaction.ReactedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            // إرسال تحديث لكل الأطراف
            var updatedReactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .GroupBy(r => r.Reaction)
                .Select(g => new { Reaction = g.Key, Count = g.Count() })
                .ToListAsync();

            await Clients.Group(messageId.ToString()).SendAsync("ReceiveReactionUpdate", messageId, updatedReactions);
        }
       

    }
}
