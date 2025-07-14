using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.IServices;

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

        //[HttpGet]
        //public IActionResult Create()
        //{
        //    var postViewModel = new PostViewModel
        //    {
        //        Categories = new SelectList(_context.Categories, "Id", "Name"),
        //    };
        //    return View(postViewModel);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequestSizeLimit(10 * 1024 * 1024)]
        //public async Task<IActionResult> Create(PostViewModel postViewModel)
        //{
        //    _logger.LogInformation("Create(PostViewModel) called.");

        //    if (!ModelState.IsValid)
        //    {
        //        _logger.LogWarning("Create(PostViewModel) called with invalid model state.");
        //        return View(postViewModel);
        //    }

        //    var allowedExtensions = _config.GetSection("uploading:allowedFileExtension").Get<List<string>>();
        //    var maxSizeMb = _config.GetValue<int>("uploading:allowedFileSize");
        //    var maxSizeBytes = maxSizeMb * 1024 * 1024;

        //    var ext = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
        //    if (!allowedExtensions.Contains(ext))
        //    {
        //        _logger.LogWarning("Invalid image format: {Extension}. Allowed: {@AllowedExtensions}", ext, allowedExtensions);
        //        ModelState.AddModelError("Image", $"Invalid image format. Allowed formats: {string.Join(", ", allowedExtensions)}");

        //        return View(postViewModel);
        //    }

        //    if (postViewModel.FeatureImage.Length > maxSizeBytes)
        //    {
        //        _logger.LogWarning("Image too large: {Size} bytes. Max allowed: {MaxSizeBytes}", postViewModel.FeatureImage.Length, maxSizeBytes);

        //        ModelState.AddModelError("Image", $"Image size cannot exceed {maxSizeMb}MB.");
        //        return View(postViewModel);
        //    }

        //    postViewModel.Post.FeatureImagePath = await _uploadFile.UploadFileToFolderAsync(postViewModel.FeatureImage);

        //    _context.Posts.Add(postViewModel.Post);
        //    await _context.SaveChangesAsync();

        //    _logger.LogInformation("Post created successfully: {@Post}", postViewModel.Post);

        //    return RedirectToAction(nameof(Index));
        //}


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
                    post.UserImageUrl = currentUser.ProfilePicture ?? "/assets/images/default-profile.jpg";
                }

                // التحقق من وجود عنوان ومحتوى للمقال
                if (string.IsNullOrEmpty(post.Title) || string.IsNullOrEmpty(post.Content))
                {
                    ModelState.AddModelError("", "Title and Content are required.");
                    return View(post);
                }

                // إذا تم رفع صورة جديدة
                if (ImageUrl != null && ImageUrl.Length > 0)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", ImageUrl.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUrl.CopyToAsync(stream);
                    }
                    post.FeatureImagePath = "/images/" + ImageUrl.FileName;
                }
                else if (post.Id != 0 && string.IsNullOrEmpty(post.FeatureImagePath))
                {
                    var existingPost = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == post.Id);
                    if (existingPost != null && !string.IsNullOrEmpty(existingPost.FeatureImagePath))
                    {
                        post.FeatureImagePath = existingPost.FeatureImagePath; // احتفظ بالصورة القديمة
                    }
                }

                else if (post.Id == 0 && string.IsNullOrEmpty(post.FeatureImagePath))
                {
                    // تعيين صورة افتراضية فقط إذا كان المقال جديدًا ولا توجد صورة
                    post.FeatureImagePath = "/assets/images/default-image.jpg";
                }

                // حفظ أو تحديث المقال
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
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving post: {ex.Message}");
                return StatusCode(500, "An error occurred while saving the post.");
            }
        }


        [HttpGet]
        public IActionResult Index(int? categoryId)
        {
            _logger.LogInformation("Index called. CategoryId = {CategoryId}", categoryId);

            var postQuery = _context.Posts.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue)
            {
                postQuery = postQuery.Where(p => p.CategoryId == categoryId);
                _logger.LogInformation("Filtering posts by CategoryId = {CategoryId}", categoryId);
            }
            var posts = postQuery.AsNoTracking().ToList();

            ViewData["Categories"] = _context.Categories.ToList();

            _logger.LogInformation("Index loaded successfully with {PostCount} posts.", posts.Count);
            return View(posts);
        }

     
        public async Task<IActionResult> Detail(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Detail: Invalid Post ID = {PostId}", id);
                return NotFound();
            }
            _logger.LogInformation("Detail called with Post ID = {PostId}", id);

            if (id == 0)
            {
                _logger.LogWarning("Detail: Invalid Post ID = {PostId}", id);
                return NotFound();
            }

            var post = _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .Include(p => p.PostLikes)
                    .ThenInclude(un => un.User)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                _logger.LogWarning("Detail: Post not found with ID = {PostId}", id);
                return NotFound();
            }

            _logger.LogInformation("Detail: Post loaded successfully with ID = {PostId}", id);
            var currentUser = await _userManager.GetUserAsync(User);
            bool userLikedPost = post.PostLikes.Any(l => l.UserId == currentUser.Id);
            ViewData["UserLikedPost"] = userLikedPost;
            ViewData["LikesCount"] = post.PostLikes.Count;
            return View(post);
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


            return RedirectToAction("Index");
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

