using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using System.Security.Claims;

public class CommentHub : Hub
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public CommentHub(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post != null && post.UserId != userIdName) 
        {
            var notification = new Notification
            {
                UserId = post.UserId, 
                Message = $"{userName} commented on your post '{post.Title}'", 
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

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
