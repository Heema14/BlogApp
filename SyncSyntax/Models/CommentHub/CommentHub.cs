using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SyncSyntax.Data;
using System.Security.Claims;

namespace SyncSyntax.Models.CommentHub
{
    [Authorize] // SignalR => make sure that exist user in website
    public class CommentHub : Hub
    {
        private readonly AppDbContext _context;

        public CommentHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task BroadcastComment(string content, int postId)
        {
            var userIdName = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var comment = new Comment
            {
                Content = content,
                CommentDate = DateTime.Now,
                UserId = userIdName,
                PostId = postId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userIdName);
            var userName = user?.UserName ?? "Unknown";

            await Clients.Group(postId.ToString()).SendAsync(
                "ReceiveComment",
                userName,
                content,
                comment.CommentDate.ToString("M/dd/yyyy, h:mm")
            );
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var postId = httpContext?.Request.Query["postId"];
            if (!string.IsNullOrEmpty(postId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, postId);
            }

            await base.OnConnectedAsync();
        }
    }

}
