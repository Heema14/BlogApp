using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.Hubs;
using System.Security.Claims;

[Authorize(Roles = "ContentCreator, Admin")]
[Area("ContentCreator")]
public class FollowingController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<FollowingController> _logger;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHubContext<PostLikeHub> _postlikeHub;

    public FollowingController(AppDbContext context, ILogger<FollowingController> logger, UserManager<AppUser> userManager, IHubContext<PostLikeHub> postlikeHub)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _postlikeHub = postlikeHub;
    }

    public IActionResult FollowingPosts(int? categoryId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var followedUsers = _context.Followings
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToList();

        var postQuery = _context.Posts
            .Include(p => p.Category)
            .Where(p => followedUsers.Contains(p.UserId) && p.IsPublished);

        if (categoryId.HasValue)
        {
            postQuery = postQuery.Where(p => p.CategoryId == categoryId);
        }

        var posts = postQuery
            .OrderByDescending(p => p.PublishedDate)
            .AsNoTracking()
            .ToList();

        var postOwnerIds = posts.Select(p => p.UserId).Distinct().ToList();

        var followingIds = _context.Followings
            .Where(f => f.FollowerId == userId && postOwnerIds.Contains(f.FollowingId))
            .Select(f => f.FollowingId)
            .ToHashSet();

        // جلب قائمة الـ SavedPosts الخاصة بالمستخدم الحالي:
        var savedPostIds = _context.SavedPosts
            .Where(sp => sp.UserId == userId)
            .Select(sp => sp.PostId)
            .ToHashSet();

        var viewModelList = posts.Select(post => new PostWithFollowStatusViewModel
        {
            Post = post,
            IsFollowing = followingIds.Contains(post.UserId),
            IsSaved = savedPostIds.Contains(post.Id)  // هنا تحدد اذا المنشور محفوظ
        }).ToList();

        ViewData["Categories"] = _context.Categories.ToList();

        var unreadNotificationsCount = _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .Count();

        ViewBag.UnreadNotificationsCount = unreadNotificationsCount;

        return View(viewModelList);
    }

    [HttpPost]
    public IActionResult Follow(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userToFollow = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (userToFollow == null)
        {
            return NotFound("User not found");
        }

        if (currentUserId == userId)
        {
            return RedirectToAction("Profile", new { userId = userId });
        }

        var follow = new Following { FollowerId = currentUserId, FollowingId = userId };

        _context.Followings.Add(follow);
        _context.SaveChanges();

        var notification = new Notification
        {
            UserId = userId,  // الشخص الذي يتم متابعته
            Message = $"{_userManager.GetUserName(User)} started following you.",  // نرسل إشعارًا للمتابع عليه
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _context.Notifications.Add(notification);
        _context.SaveChanges();

        return RedirectToAction("Profile", new { userId = userId });
    }

    [HttpPost]
    public IActionResult Unfollow(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var follow = _context.Followings
            .FirstOrDefault(f => f.FollowerId == currentUserId && f.FollowingId == userId);

        if (follow != null)
        {
            try
            {
                _context.Followings.Remove(follow);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while unfollowing user");
            }
        }

        return RedirectToAction("Profile", new { userId = userId });
    }

    public IActionResult Profile(string userId)
    {
        Console.WriteLine("UserID received: " + userId);

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return NotFound("User not found");
        }

        var currentUser = _userManager.GetUserAsync(User).Result; // جلب المستخدم الحالي
        var currentUserId = currentUser?.Id; // معرف المستخدم الحالي

        var followersCount = _context.Followings
            .Count(f => f.FollowingId == userId);

        var postsCount = _context.Posts
            .Count(p => p.UserId == userId);

        var posts = _context.Posts
            .Where(p => p.UserId == userId)
            .Include(p => p.Category)
            .OrderByDescending(p => p.PublishedDate)
            .ToList();

        var followingCount = _context.Followings
            .Count(f => f.FollowerId == userId);

        var model = new ProfileViewModel
        {
            User = user,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            PostsCount = postsCount,
            Posts = posts,
            Bio = user.Bio,
            CurrentUserId = currentUserId // إضافة معرف المستخدم الحالي إلى الـ ViewModel
        };

        return View(model);
    }


    public async Task<IActionResult> Users()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        var users = await _context.Users
            .Where(u => u.Id != currentUser.Id)
            .ToListAsync();

        return View(users);
    }


    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Like(int postId)
    {
        try
        {
            var userId = _userManager.GetUserId(User);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return NotFound();
            }

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            bool userLiked;

            if (existingLike == null)
            {
                // Add like
                var like = new PostLike
                {
                    PostId = postId,
                    UserId = userId,
                    LikedAt = DateTime.Now
                };

                _context.PostLikes.Add(like);
                post.LikesCount++;
                userLiked = true;
            }
            else
            {
                try
                {
                    // Remove like
                    _context.PostLikes.Remove(existingLike);
                    post.LikesCount--;
                    userLiked = false;
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Json(new { success = false, message = "The like you tried to remove no longer exists." });
                }
            }
            await _context.SaveChangesAsync();

            // بث تحديث اللايك بالـ SignalR
            await _postlikeHub.Clients.Group(postId.ToString())
                .SendAsync("ReceiveLike", postId, post.LikesCount);

            return Json(new { success = true, likesCount = post.LikesCount, userLiked });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, ex.Message });
        }

    }
}
