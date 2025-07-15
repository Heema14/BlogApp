using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using System.Security.Claims;



[Authorize(Roles = "ContentCreator, Admin")]
[Area("ContentCreator")]
public class FollowingController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<FollowingController> _logger;
    private readonly UserManager<AppUser> _userManager;

    public FollowingController(AppDbContext context, ILogger<FollowingController> logger, UserManager<AppUser> userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
    }

    public IActionResult FollowingPosts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // جلب قائمة المتابعين للمستخدم الحالي
        var followedUsers = _context.Followings
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToList();

        // جلب البوستات من المستخدمين الذين يتابعهم المستخدم الحالي
        var posts = _context.Posts
            .Where(p => followedUsers.Contains(p.UserId)) // هنا تم تعديل UserName إلى UserId
            .Include(p => p.Category)
            .OrderByDescending(p => p.PublishedDate)
            .ToList();

        return View(posts);
    }

    [HttpPost]
    public IActionResult Follow(string userId)
    {
       
        if (Request.Method != "POST")
        {
            return BadRequest(); // يضمن أنه يتم قبول الطلبات من نوع POST فقط
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation($"Attempting to follow user with ID: {userId}");
        _logger.LogInformation($"Current logged-in user ID: {currentUserId}");

        // تأكد من أن المستخدم الذي تريد متابعته موجود في قاعدة البيانات
        var userToFollow = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (userToFollow == null)
        {
            return NotFound("User not found");  // إذا لم يكن المستخدم موجودًا، يمكنك إرجاع رسالة خطأ
        }

        // تأكد من أن المستخدم لا يتابع نفسه
        if (currentUserId == userId)
        {
            return RedirectToAction("Profile", new { userId = userId });
        }

        var follow = new Following { FollowerId = currentUserId, FollowingId = userId };

        try
        {
            _context.Followings.Add(follow);
            _context.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            var innerException = ex.InnerException?.Message ?? "No inner exception";
            _logger.LogError($"DbUpdateException: {innerException}");
            return BadRequest("An error occurred while following the user.");
        }


        return RedirectToAction("Profile", new { userId = userId });
    }


    [HttpPost]  // تأكد من إضافة هذا السطر
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

}
