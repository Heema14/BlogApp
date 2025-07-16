using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
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
        public PostController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<PostController> logger, IUploadFileService uploadFile, IConfiguration config, UserManager<AppUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _uploadFile = uploadFile;
            _config = config;
            _userManager = userManager;

        }


        [HttpPost]
        public IActionResult TogglePublishStatus(int postId)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == postId);
            if (post == null)
                return NotFound();

            post.IsPublished = !post.IsPublished;
            _context.SaveChanges();
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
                _logger.LogInformation($"Received post data: {post.Id}, {post.Title}, {post.Description}, {post.CategoryId}, {post.Content}");

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
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", ImageUrl.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUrl.CopyToAsync(stream);
                    }
                    post.FeatureImagePath = "/images/uploadImgs" + ImageUrl.FileName;
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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("PostDetail", viewModel);
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
        public async Task<IActionResult> Like(int postId)
        {
            try
            {
                // الحصول على المستخدم الحالي
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(); // إذا كان المستخدم غير موجود
                }

                // العثور على الـ Post بواسطة الـ postId
                var post = await _context.Posts.FindAsync(postId);
                if (post == null)
                {
                    return Json(new { success = false, message = "Post not found." });
                }

                _logger.LogInformation($"User {currentUser.UserName} with ID {currentUser.Id} is attempting to like/unlike post {postId}.");

                // تحقق إذا كان المستخدم قد سبق له إعجاب هذا الـ Post
                var existingLike = await _context.PostLikes
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUser.Id);

                if (existingLike != null)
                {
                    // إذا كان الـ Like موجودًا، قم بإزالته
                    _context.PostLikes.Remove(existingLike);
                }
                else
                {
                    // إذا لم يكن الـ Like موجودًا، أضفه
                    var postLike = new PostLike
                    {
                        PostId = postId,
                        UserId = currentUser.Id,
                        LikedAt = DateTime.Now
                    };

                    _context.PostLikes.Add(postLike);
                }

                // تحديث عدد اللايكات بعد إضافة أو إزالة اللايك
                post.LikesCount = await _context.PostLikes.CountAsync(l => l.PostId == postId);

                // حفظ التغييرات في قاعدة البيانات
                await _context.SaveChangesAsync();

                // استرجاع حالة إعجاب المستخدم بالـ Post
                var userLiked = post.PostLikes.Any(l => l.UserId == currentUser.Id);

                return Json(new { success = true, likesCount = post.LikesCount, userLiked });
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException?.Message ?? "No inner exception message.";
                _logger.LogError($"Database error in Like action: {dbEx.Message}. Inner exception: {innerException}");

                return Json(new { success = false, message = "An error occurred while saving your like. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Like action: {ex.Message}");
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
        }

    }
}

