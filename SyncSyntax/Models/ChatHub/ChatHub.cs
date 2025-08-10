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
        private readonly InMemoryChatCacheService _cache;

        public ChatHub(AppDbContext context, UserManager<AppUser> userManager, InMemoryChatCacheService cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
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
            
            string cacheKey = $"chat:{senderId}:{receiverId}";
            var updatedMessages = await _context.Messages
                .Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == senderId))
                .OrderByDescending(m => m.SentAt)
                .Take(100)
                .ToListAsync();

            await _cache.SetMessagesAsync(cacheKey, updatedMessages);


            await Groups.AddToGroupAsync(Context.ConnectionId, newMessage.Id.ToString());


            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == newMessage.Id)
                .GroupBy(r => r.Reaction)
                .Select(g => new { Reaction = g.Key, Count = g.Count() })
                .ToListAsync();


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
            var userId = Context.UserIdentifier;

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

                    _context.MessageReactions.Remove(existingReaction);
                }
                else
                {

                    existingReaction.Reaction = reaction;
                    existingReaction.ReactedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();


            var updatedReactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .GroupBy(r => r.Reaction)
                .Select(g => new { Reaction = g.Key, Count = g.Count() })
                .ToListAsync();

            await Clients.Group(messageId.ToString()).SendAsync("ReceiveReactionUpdate", messageId, updatedReactions);
        }


    }
}
