using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.IServices;
using SyncSyntax.Models.ViewModels;
using System.Net;
using System.Text.RegularExpressions;

namespace SyncSyntax.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PostController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _config;
        private readonly IUploadFileService _uploadFile;

        public PostController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<PostController> logger, IUploadFileService uploadFile, IConfiguration config)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _uploadFile = uploadFile;
            _config = config;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var postViewModel = new PostViewModel
            {
                Categories = new SelectList(_context.Categories, "Id", "Name"),
            };
            return View(postViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Create(PostViewModel postViewModel)
        {
            _logger.LogInformation("Create(PostViewModel) called.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create(PostViewModel) called with invalid model state.");
                return View(postViewModel);
            }

            var allowedExtensions = _config.GetSection("uploading:allowedFileExtension").Get<List<string>>();
            var maxSizeMb = _config.GetValue<int>("uploading:allowedFileSize");
            var maxSizeBytes = maxSizeMb * 1024 * 1024;

            var ext = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                _logger.LogWarning("Invalid image format: {Extension}. Allowed: {@AllowedExtensions}", ext, allowedExtensions);
                ModelState.AddModelError("Image", $"Invalid image format. Allowed formats: {string.Join(", ", allowedExtensions)}");

                return View(postViewModel);
            }

            if (postViewModel.FeatureImage.Length > maxSizeBytes)
            {
                _logger.LogWarning("Image too large: {Size} bytes. Max allowed: {MaxSizeBytes}", postViewModel.FeatureImage.Length, maxSizeBytes);

                ModelState.AddModelError("Image", $"Image size cannot exceed {maxSizeMb}MB.");
                return View(postViewModel);
            }

            postViewModel.Post.FeatureImagePath = await _uploadFile.UploadFileToFolderAsync(postViewModel.FeatureImage);

            _context.Posts.Add(postViewModel.Post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post created successfully: {@Post}", postViewModel.Post);

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var postViewModel = new PostViewModel
            {
                Categories = new SelectList(_context.Categories, "Id", "Name"),
                Post = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id),
            };
            return View(postViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PostViewModel postViewModel)
        {
            _logger.LogInformation("Edit(PostViewModel) called for Post ID = {PostId}", postViewModel.Post.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Edit(PostViewModel): ModelState is invalid for Post ID = {PostId}", postViewModel.Post.Id);

                return View(postViewModel);
            }

            var postFromDb = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(
                p => p.Id == postViewModel.Post.Id);

            if (postFromDb == null)
            {
                _logger.LogWarning("Edit(PostViewModel): Post not found with ID = {PostId}", postViewModel.Post.Id);
                return NotFound();
            }

            // when add new image
            if (postViewModel.FeatureImage != null)
            {
                var allowedExtensions = _config.GetSection("uploading:allowedFileExtension").Get<List<string>>();
                var maxSizeMb = _config.GetValue<int>("uploading:allowedFileSize");
                var maxSizeBytes = maxSizeMb * 1024 * 1024;

                var ext = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("Edit(PostViewModel): Invalid image format: {Extension}. Allowed: {@AllowedExtensions}",
                        ext, allowedExtensions);

                    ModelState.AddModelError("Image", $"Invalid image format. Allowed formats: {string.Join(", ", allowedExtensions)}");

                    return View(postViewModel);
                }

                if (postViewModel.FeatureImage.Length > maxSizeBytes)
                {
                    _logger.LogWarning("Edit(PostViewModel): Image too large: {Size} bytes. Max allowed: {MaxSizeBytes}",
                        postViewModel.FeatureImage.Length, maxSizeBytes);

                    ModelState.AddModelError("Image", $"Image size cannot exceed {maxSizeMb}MB.");
                    return View(postViewModel);
                }

                // delete old image if it exist
                if (!string.IsNullOrEmpty(postFromDb.FeatureImagePath))
                {
                    var existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "images",
                        Path.GetFileName(postFromDb.FeatureImagePath));

                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                        _logger.LogInformation("Old image deleted: {FilePath}", existingFilePath);
                    }
                }

                // add new image
                postViewModel.Post.FeatureImagePath = await _uploadFile.UploadFileToFolderAsync(postViewModel.FeatureImage);

                _logger.LogInformation("New image uploaded for Post ID = {PostId}", postViewModel.Post.Id);
            }
            else
            {
                postViewModel.Post.FeatureImagePath = postFromDb.FeatureImagePath;
            }

            _context.Posts.Update(postViewModel.Post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post updated successfully: {@Post}", postViewModel.Post);
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        [AllowAnonymous]
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


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Delete(GET) called with ID = {PostId}", id);

            var postFromDb = await _context.Posts.FindAsync(id);
            if (postFromDb == null)
            {
                _logger.LogWarning("Delete(GET): Post not found with ID = {PostId}", id);
                return NotFound();
            }

            _logger.LogInformation("Delete(GET): Post found with ID = {PostId}", id);
            return View(postFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            _logger.LogInformation("DeleteConfirm(POST) called with ID = {PostId}", id);

            if (id < 0)
            {
                _logger.LogWarning("DeleteConfirm(POST): Invalid post ID = {PostId}", id);
                return BadRequest();
            }

            var postFromDb = await _context.Posts.FindAsync(id);
            if (postFromDb == null)
            {
                _logger.LogWarning("DeleteConfirm(POST): Post not found with ID = {PostId}", id);
                return NotFound();
            }

            if (!string.IsNullOrEmpty(postFromDb.FeatureImagePath))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", Path.GetFileName(postFromDb.FeatureImagePath));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                    _logger.LogInformation("Deleted image file: {ImagePath}", imagePath);
                }
            }
            _context.Posts.Remove(postFromDb);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post deleted successfully with ID = {PostId}", id);
            return RedirectToAction(nameof(Index));
        }


        [AllowAnonymous]
        public IActionResult Detail(int id)
        {
            _logger.LogInformation("Detail called with Post ID = {PostId}", id);

            if (id == 0)
            {
                _logger.LogWarning("Detail: Invalid Post ID = {PostId}", id);
                return NotFound();
            }

            var post = _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Comments)
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                _logger.LogWarning("Detail: Post not found with ID = {PostId}", id);
                return NotFound();
            }

            _logger.LogInformation("Detail: Post loaded successfully with ID = {PostId}", id);
            return View(post);
        }


        //[HttpPost]
        //[Authorize(Roles = "Admin,User")]
        //public JsonResult AddComment([FromBody] Comment comment)
        //{
        //    var currentUserName = User.Identity?.Name ?? "Anonymous..";

        //    _logger.LogInformation("AddComment called by user = {UserName}", currentUserName);

        //    if (ModelState.IsValid)
        //    {
        //        comment.UserName = currentUserName; // User in system
        //        comment.CommentDate = DateTime.Now;

        //        _context.Comments.Add(comment);
        //        _context.SaveChanges();

        //        _logger.LogInformation("Comment added successfully to Post ID = {PostId} by {UserName}",
        //      comment.PostId, comment.UserName);

        //        return Json(new
        //        {
        //            userName = comment.UserName,
        //            commentDate = comment.CommentDate.ToString("M/dd/yyyy, h:m"),
        //            content = comment.Content
        //        });
        //    }

        //    _logger.LogError("Error while adding comment to Post ID = {PostId}", comment.PostId);
        //    return Json(new { success = false, message = "Invalid data" });
        //}

    }
}

