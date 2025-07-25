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
                newMessage.IsPinned
                );

            await Clients.User(senderUser.Id).SendAsync("ReceiveMessage",
                senderUser.Id,
                message,
                newMessage.Id,
                newMessage.SentAt,
                newMessage.IsRead,
                newMessage.IsPinned
                );
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


      

    }
}
