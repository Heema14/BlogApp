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
using System.Text.RegularExpressions;

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
            var currentUserId = _userManager.GetUserId(User);

            var unreadNotificationsCount = _context.Notifications
           .Where(n => n.UserId == currentUserId && !n.IsRead)
           .Count();

            ViewBag.UnreadNotificationsCount = unreadNotificationsCount;
            ViewBag.UnreadCount = _context.Messages
                .Count(m => m.ReceiverId == currentUserId && !m.IsRead);

            return View(new Post());
        }


        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                TempData["Error"] = "The post you’re trying to edit was not found.";
                return RedirectToAction("Profile", "Following");
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

                    TempData["Success"] = "Post created successfully 🎉";
                }
                else
                {
                    post.UpdatedAt = DateTime.Now;
                    _context.Update(post);

                    TempData["Success"] = "Post updated successfully ✏️";
                }

                var hashtags = ExtractHashtags(post.Content);
                post.Tags = string.Join(",", hashtags);

                await _context.SaveChangesAsync();
                return RedirectToAction("Profile", "Following", new { userId = post.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving post: {ex.Message}");
                TempData["Error"] = "An unexpected error occurred while saving the post. Please try again.";
                return RedirectToAction("Profile", "Following");
            }
        }


        [HttpGet]
        public IActionResult Explore(string searchTerm, string filterBy, int? categoryId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var postQuery = _context.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.PostLikes)
                .Where(p => p.IsPublished);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                switch (filterBy)
                {
                    case "title":
                        postQuery = postQuery.Where(p => p.Title.Contains(searchTerm));
                        break;
                    case "category":
                        postQuery = postQuery.Where(p => p.Category.Name.Contains(searchTerm));
                        break;
                    case "publisher":
                        postQuery = postQuery.Where(p => p.UserName.Contains(searchTerm));
                        break;
                    case "date":
                        if (DateTime.TryParse(searchTerm, out var date))
                            postQuery = postQuery.Where(p => p.PublishedDate.Date == date.Date);
                        break;
                    case "tag":
                        postQuery = postQuery.Where(p => !string.IsNullOrEmpty(p.Tags) && p.Tags.Contains(searchTerm.ToLower()));
                        break;

                }
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

            var savedPostIds = _context.SavedPosts
                .Where(sp => sp.UserId == currentUserId && posts.Select(p => p.Id).Contains(sp.PostId))
                .Select(sp => sp.PostId)
                .ToHashSet();

            var viewModelList = posts.Select(post => new PostWithFollowStatusViewModel
            {
                Post = post,
                IsFollowing = followingIds.Contains(post.UserId),
                IsSaved = savedPostIds.Contains(post.Id)   
            }).ToList();

            ViewData["Categories"] = _context.Categories.ToList();

            var unreadNotificationsCount = _context.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .Count();

            ViewBag.UnreadNotificationsCount = unreadNotificationsCount;


            ViewBag.UnreadCount = _context.Messages
                .Count(m => m.ReceiverId == currentUserId && !m.IsRead);

            //ViewBag.Users = _context.Users.Select(u => new { id = u.Id, name = u.UserName }).ToList();
            //ViewBag.Categories = _context.Categories.Select(c => new { id = c.Id, name = c.Name }).ToList();
            //ViewBag.Posts = _context.Posts.Select(p => new { id = p.Id, title = p.Title }).ToList();

            return View(viewModelList);
        }


        [HttpGet]
        public JsonResult SearchSuggestions(string term, string filterBy)
        {
            var suggestions = new List<string>();

            switch (filterBy)
            {
                case "title":
                    suggestions = _context.Posts
                        .Where(p => p.Title.Contains(term))
                        .Select(p => p.Title)
                        .Distinct()
                        .Take(10)
                        .ToList();
                    break;

                case "category":
                    suggestions = _context.Categories
                        .Where(c => c.Name.Contains(term))
                        .Select(c => c.Name)
                        .Distinct()
                        .Take(10)
                        .ToList();
                    break;

                case "publisher":
                    suggestions = _context.Users
                        .Where(u => u.UserName.Contains(term))
                        .Select(u => u.UserName)
                        .Distinct()
                        .Take(10)
                        .ToList();
                    break;

                case "date":
                    suggestions = _context.Posts
                        .AsEnumerable()
                        .Where(p => p.PublishedDate.ToString().Contains(term))
                        .Select(p => p.PublishedDate.ToString("yyyy-MM-dd"))
                        .Distinct()
                        .Take(10)
                        .ToList();
                    break;

                case "tag":
                    suggestions = _context.Posts
                        .Where(p => !string.IsNullOrEmpty(p.Tags))
                        .AsEnumerable() // ✅ نقل التنفيذ للذاكرة
                        .SelectMany(p => p.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        .Select(tag => tag.Trim())
                        .Where(tag => tag.ToLower().StartsWith(term))
                        .Distinct()
                        .Take(10)
                        .ToList();
                    break;

            }

            return Json(suggestions);
        }


        [HttpGet]
        public IActionResult MyPosts(int? categoryId)
        {

            var userName = User.Identity.Name;

            if (string.IsNullOrEmpty(userName))
            {

                _logger.LogWarning("User is not authenticated.");
                return RedirectToAction("Login", "Account");
            }


            var postQuery = _context.Posts
                                    .Where(p => p.UserName == userName)
                                    .Include(p => p.PostLikes)
                                    .Include(p => p.Category)
                                    .AsQueryable();

            if (categoryId.HasValue)
            {

                postQuery = postQuery.Where(p => p.CategoryId == categoryId);
            }

            var posts = postQuery.AsNoTracking().ToList();


            ViewBag.Categories = _context.Categories
                .AsNoTracking()
                .Select(c => new { id = c.Id, name = c.Name })
                .ToList();

            return View(posts);
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
                post.Views += 1;
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();

                return PartialView("PostDetail", viewModel);
            }


            return View(viewModel);
        }


        [HttpPost]
        public IActionResult AddComment(int postId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            var comment = new Comment
            {
                PostId = postId,
                Content = content,
                UserId = userId,
                CommentDate = DateTime.Now
            };
            _context.Comments.Add(comment);
            _context.SaveChanges();

            return Json(new
            {
                commentId = comment.Id,
                userName,
                content = comment.Content,
                date = comment.CommentDate.ToString("M/dd/yyyy, h:mm tt")
            });
        }



        [HttpGet]
        public IActionResult Delete(int id)
        {
            var post = _context.Posts.FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                TempData["Error"] = "Post not found. It may have already been deleted.";
                return RedirectToAction("Profile", "Following");
            }

            _context.Posts.Remove(post);
            _context.SaveChanges();

            TempData["Success"] = "Post deleted successfully.";
            return RedirectToAction("Profile", "Following");
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like(int postId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "You must be logged in." });

            var post = await _context.Posts
                .Include(p => p.PostLikes)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return Json(new { success = false, message = "Post not found." });

            var existingLike = post.PostLikes.FirstOrDefault(l => l.UserId == userId);
            bool userLiked;

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);

                if (post.LikesCount > 0)
                    post.LikesCount--;

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
                post.LikesCount++;
                userLiked = true;
            }

            await _context.SaveChangesAsync();
        
            await _postlikeHub.Clients.Group(postId.ToString())
                .SendAsync("ReceiveLike", postId, post.LikesCount, userId);

            return Json(new { success = true, likesCount = post.LikesCount, userLiked });
        }


        public async Task<IActionResult> Saved()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var savedPosts = await _context.SavedPosts
                .Where(sp => sp.UserId == userId)
                .Include(sp => sp.Post)
                    .ThenInclude(p => p.Category)
                .Include(sp => sp.Post)
                    .ThenInclude(p => p.Comments)
                .ToListAsync();
            var currentUserId = _userManager.GetUserId(User);

            var unreadNotificationsCount = _context.Notifications
           .Where(n => n.UserId == currentUserId && !n.IsRead)
           .Count();

            ViewBag.UnreadNotificationsCount = unreadNotificationsCount;
            ViewBag.UnreadCount = _context.Messages
                .Count(m => m.ReceiverId == currentUserId && !m.IsRead);
            return View(savedPosts);
        }


        [HttpPost]
        public async Task<IActionResult> ToggleSave(int postId, string returnPage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists)
            {
                TempData["Error"] = "Post not found.";
                return RedirectToAction("Explore", "Post", new { area = "ContentCreator" });
            }

            var existing = await _context.SavedPosts
                .FirstOrDefaultAsync(sp => sp.PostId == postId && sp.UserId == userId);

            if (existing != null)
            {
                var toRemove = new SavedPost { PostId = postId, UserId = userId };
                _context.SavedPosts.Attach(toRemove);
                _context.SavedPosts.Remove(toRemove);

                TempData["Info"] = "Post removed from your saved list.";
            }
            else
            {
                var saved = new SavedPost { UserId = userId, PostId = postId };
                _context.SavedPosts.Add(saved);

                TempData["Success"] = "Post saved successfully!";
            }

            await _context.SaveChangesAsync();

            if (returnPage == "FollowingPosts")
            {
                return RedirectToAction("FollowingPosts", "Following", new { area = "ContentCreator" });
            }
            else if (returnPage == "Explore")
            {
                return RedirectToAction("Explore", "Post", new { area = "ContentCreator" });
            }
            else
            {
                return RedirectToAction("Detail", "Post", new { area = "ContentCreator", id = postId });
            }
        }


        private List<string> ExtractHashtags(string content)
        {
            var hashtags = new List<string>();
            if (!string.IsNullOrEmpty(content))
            {

                var matches = Regex.Matches(content, @"#\w+");
                foreach (Match match in matches)
                {

                    hashtags.Add(match.Value.Substring(1).ToLower());
                }
            }
            return hashtags.Distinct().ToList();
        }


    }
}

