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
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(content))
            return;

        var comment = new Comment
        {
            Content = content,
            CommentDate = DateTime.UtcNow,
            UserId = userId,
            PostId = postId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        var userName = user?.UserName ?? "Unknown";

        // إرسال التعليق لكل من في الجروب
        await Clients.Group(postId.ToString())
            .SendAsync("ReceiveComment",
            userName,
            content,
            comment.CommentDate.ToString("M/dd/yyyy, h:mm tt"),
            comment.Id,
            comment.UserId == Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }


    public override async Task OnConnectedAsync()
    {
        var postId = Context.GetHttpContext()?.Request.Query["postId"];
        if (!string.IsNullOrEmpty(postId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, postId);
        }
        await base.OnConnectedAsync();
    }


    public async Task DeleteComment(int commentId)
    {
        var comment = await _context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (comment == null || comment.UserId != userId)
            return; // فقط صاحب التعليق يمكنه الحذف

        int postId = comment.PostId;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        // بث حذف التعليق لكل المستخدمين في الجروب
        await Clients.Group(postId.ToString())
            .SendAsync("CommentDeleted", commentId);
    }



}
