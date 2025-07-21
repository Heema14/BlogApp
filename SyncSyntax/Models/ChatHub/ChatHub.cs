using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using SyncSyntax.Data;
using SyncSyntax.Models;
using System;
using System.Threading.Tasks;

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

        public async Task SendMessage(string senderUserName, string receiverId, string message)
        {
            var senderUser = await _userManager.FindByNameAsync(senderUserName);
            if (senderUser == null) return;

            var newMessage = new Message
            {
                SenderId = senderUser.Id,
                ReceiverId = receiverId,
                Content = message,
                SentAt = DateTime.UtcNow,
                IsRead = false // جديد، تأكد أن الحقل موجود في الموديل
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            // إرسال لجميع الأطراف (المرسل والمستقبل) حتى يحدث عند كلا الطرفين فوراً
            await Clients.User(receiverId).SendAsync("ReceiveMessage",
                senderUserName,
                newMessage.Content,
                newMessage.Id,
                newMessage.SentAt,
                newMessage.IsRead);

            await Clients.User(senderUser.Id).SendAsync("ReceiveMessage",
                senderUserName,
                newMessage.Content,
                newMessage.Id,
                newMessage.SentAt,
                newMessage.IsRead);
        }

    }
}
