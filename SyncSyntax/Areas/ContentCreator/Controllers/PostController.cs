using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.Hubs;
using SyncSyntax.Models.IServices;
using System.Security.Claims;

namespace SyncSyntax.Areas.ContentCreator.Controllers
{
    [Authorize(Roles = "ContentCreator, Admin")]
    [Area("ContentCreator")]
    public class PostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PostController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _config;
        private readonly IUploadFileService _uploadFile;
        private readonly UserManager<AppUser> _userManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<PostLikeHub> _postlikeHub;

        public PostController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<PostController> logger, IUploadFileService uploadFile, IConfiguration config, UserManager<AppUser> userManager, IServiceProvider serviceProvider, IHubContext<PostLikeHub> postlikeHub)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _uploadFile = uploadFile;
            _config = config;
            _userManager = userManager;
            _serviceProvider = serviceProvider;
            _postlikeHub = postlikeHub;
        }

        [HttpPost]
        public async Task<IActionResult> TogglePublishStatus(int postId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            post.IsPublished = !post.IsPublished;
            _context.SaveChanges();

            if (post.IsPublished)
            {
                var userWhoPublished = await _userManager.FindByIdAsync(post.UserId);

                if (userWhoPublished != null)
                {
                    var followers = await _context.Followings
                                                  .Where(f => f.FollowingId == userWhoPublished.Id)
                                                  .Select(f => f.FollowerId)
                                                  .ToListAsync();

                    foreach (var followerId in followers)
                    {
                        var follower = await _userManager.FindByIdAsync(followerId);
                        if (follower != null)
                        {
                            var notification = new Notification
                            {
                                UserId = followerId,
                                Message = $"{userWhoPublished.UserName} published a new post: '{post.Title}'",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            };

                            _context.Notifications.Add(notification);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, isPublished = post.IsPublished });
        }

        public IActionResult Create()
        {
            var categories = _context.Categories.ToList();
            ViewBag.Categories = categories;
            return View(new Post());
        }


        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var categories = _context.Categories.ToList();
            ViewBag.Categories = categories;
            return View("Create", post);
        }

        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Post post, IFormFile? ImageUrl)
        {
            try
            {
                _logger.LogInformation($"Received post data: {post.Id}, {post.Title}, {post.CategoryId}, {post.Content}");

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    post.UserName = currentUser.UserName;
                    post.UserImageUrl = currentUser.ProfilePicture ?? "/images/uploadImgs/default-profile.jpg";
                    post.UserId = currentUser.Id;
                }


                if (string.IsNullOrEmpty(post.Title) || string.IsNullOrEmpty(post.Content))
                {
                    ModelState.AddModelError("", "Title and Content are required.");
                    return View(post);
                }


                if (ImageUrl != null && ImageUrl.Length > 0)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images/uploadImgs", ImageUrl.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUrl.CopyToAsync(stream);
                    }
                    post.FeatureImagePath = "/images/uploadImgs/" + ImageUrl.FileName;
                }
                else if (post.Id != 0 && string.IsNullOrEmpty(post.FeatureImagePath))
                {
                    var existingPost = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == post.Id);
                    if (existingPost != null && !string.IsNullOrEmpty(existingPost.FeatureImagePath))
                    {
                        post.FeatureImagePath = existingPost.FeatureImagePath;
                    }
                }
                else if (post.Id == 0 && string.IsNullOrEmpty(post.FeatureImagePath))
                {

                    post.FeatureImagePath = "/images/uploadImgs/default-image.jpg";
                }


                if (post.Id == 0)
                {
                    post.CreatedAt = DateTime.Now;
                    post.UpdatedAt = null;
                    post.Views = 0;

                    _context.Add(post);
                }
                else
                {
                    post.UpdatedAt = DateTime.Now;
                    _context.Update(post);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Profile", "Following", new { userId = post.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving post: {ex.Message}");
                return StatusCode(500, "An error occurred while saving the post.");
            }
        }


        [HttpGet]
        public IActionResult Explore(int? categoryId)
        {

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var postQuery = _context.Posts
                .Include(p => p.Category)
                .Where(p => p.IsPublished);

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
                .Where(f => f.FollowerId == currentUserId && postOwnerIds.Contains(f.FollowingId))
                .Select(f => f.FollowingId)
                .ToHashSet();


            var viewModelList = posts.Select(post => new PostWithFollowStatusViewModel
            {
                Post = post,
                IsFollowing = followingIds.Contains(post.UserId)
            }).ToList();

            ViewData["Categories"] = _context.Categories.ToList();

            var unreadNotificationsCount = _context.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .Count();


            ViewBag.UnreadNotificationsCount = unreadNotificationsCount;

            return View(viewModelList);
        }

        [HttpGet]
        public IActionResult MyPosts(int? categoryId)
        {
            // الحصول على اسم المستخدم من المصادقة
            var userName = User.Identity.Name; // استخدم User.Identity.Name للحصول على الـ UserName من الـ Claims

            if (string.IsNullOrEmpty(userName))
            {
                // إذا لم يكن المستخدم مسجل دخول
                _logger.LogWarning("User is not authenticated.");
                return RedirectToAction("Login", "Account"); // إعادة التوجيه إلى صفحة تسجيل الدخول
            }

            // استعلام البوستات الخاصة بالمستخدم بناءً على UserName
            var postQuery = _context.Posts
                                    .Where(p => p.UserName == userName) // تصفية البوستات بناءً على الـ UserName
                                    .Include(p => p.Category) // إذا كنت بحاجة لعرض الفئة
                                    .AsQueryable();

            if (categoryId.HasValue)
            {
                // تصفية إضافية حسب الـ CategoryId
                postQuery = postQuery.Where(p => p.CategoryId == categoryId);
            }

            var posts = postQuery.AsNoTracking().ToList(); // جلب البوستات دون تتبع التغييرات

            // إرسال الفئات إلى الـ View (إذا كنت بحاجة إليها لعرضها في قائمة الفئات مثلاً)
            ViewBag.Categories = _context.Categories
                .AsNoTracking()
                .Select(c => new { id = c.Id, name = c.Name })
                .ToList();

            return View(posts); // عرض البوستات في الـ View
        }

        public async Task<IActionResult> Detail(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Detail: Invalid Post ID = {PostId}", id);
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .Include(p => p.PostLikes)
                    .ThenInclude(pl => pl.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                _logger.LogWarning("Detail: Post not found with ID = {PostId}", id);
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            bool isFollowing = false;
            bool userLikedPost = false;

            if (currentUser != null)
            {
                isFollowing = _context.Followings.Any(f =>
                    f.FollowerId == currentUser.Id && f.FollowingId == post.UserId);

                userLikedPost = post.PostLikes.Any(l => l.UserId == currentUser.Id);
            }

            var viewModel = new PostDetailViewModel
            {
                Post = post,
                IsFollowing = isFollowing,
                UserLikedPost = userLikedPost
            };

            // التأكد من أن الـ Request هو Ajax
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("PostDetail", viewModel);  // ارجع الـ Partial View
            }

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }


            _context.Posts.Remove(post);
            _context.SaveChanges();


            return RedirectToAction("Profile", "Following");
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Like(int postId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "You must be logged in." });
            }

            var post = _context.Posts.Include(p => p.PostLikes).FirstOrDefault(p => p.Id == postId);

            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var existingLike = post.PostLikes.FirstOrDefault(l => l.UserId == userId);
            bool userLiked;

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);
                post.LikesCount = Math.Max(0, post.LikesCount - 1);
                userLiked = false;
            }
            else
            {
                _context.PostLikes.Add(new PostLike
                {
                    PostId = postId,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                });
                post.LikesCount += 1;
                userLiked = true;
            }

            await _context.SaveChangesAsync();

            // 🔔 إرسال التحديث عبر SignalR
            await _postlikeHub.Clients.Group(postId.ToString())
                .SendAsync("ReceiveLike", postId, post.LikesCount, userId);

            return Json(new
            {
                success = true,
                likesCount = post.LikesCount,
                userLiked = userLiked
            });
        }

    }
}

