using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SyncSyntax.Data;

namespace SyncSyntax.Models.CommentHub
{
    [Authorize] // SignalR hub will require authentication
    public class CommentHub : Hub
    {
        private readonly AppDbContext _context;

        public CommentHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task BroadcastComment(string content, int postId)
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            var commentDate = DateTime.Now;

            var comment = new Comment
            {
                Content = content,
                CommentDate = commentDate,
                UserName = userName,
                PostId = postId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // إرسال التعليق للمجموعة حسب رقم المنشور
            await Clients.Group(postId.ToString()).SendAsync(
                "ReceiveComment",
                userName,
                content,
                commentDate.ToString("M/dd/yyyy, h:mm")
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
