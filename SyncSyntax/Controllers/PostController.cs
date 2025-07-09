using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;
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
        private readonly string[] _allowedExtension = { ".jpg", ".jpeg", ".png" };

        public PostController(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<PostController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
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
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Create(PostViewModel postViewModel)
        {
            _logger.LogInformation("Create(PostViewModel) called.");

            if (ModelState.IsValid)
            {
                var inputFileExtension = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
                bool isAllowed = _allowedExtension.Contains(inputFileExtension);
                if (!isAllowed)
                {
                    _logger.LogWarning("Invalid image format: {Extension}. Allowed: {@AllowedExtensions}",
                   inputFileExtension, _allowedExtension);

                    ModelState.AddModelError("Image", "Invalid image format. Allowed formats are .jpg, .jpeg, .png");
                    return View(postViewModel);
                }
                postViewModel.Post.FeatureImagePath = await UploadFileToFolder(postViewModel.FeatureImage);
                _context.Posts.Add(postViewModel.Post);
                _context.SaveChanges();

                _logger.LogInformation("Post created successfully: {@Post}", postViewModel.Post);

                return RedirectToAction(nameof(Index));
            }
            else
            {
                _logger.LogWarning("Create(PostViewModel) called with invalid model state: {@ModelState}", ModelState);
            }

            return View(postViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var postViewModel = new PostViewModel
            {
                Categories = new SelectList(_context.Categories, "Id", "Name"),
                Post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id),
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

            if (postViewModel.FeatureImage != null)
            {
                var inputFileExtension = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
                bool isAllowed = _allowedExtension.Contains(inputFileExtension);

                if (!isAllowed)
                {
                    _logger.LogWarning("Edit(PostViewModel): Invalid image format: {Extension}. Allowed: {@AllowedExtensions}",
                 inputFileExtension, _allowedExtension);

                    ModelState.AddModelError("Image", "Invalid image format. Allowed formats are .jpg, .jpeg, .png");
                    return View(postViewModel);
                }

                var existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "images",
                    Path.GetFileName(postFromDb.FeatureImagePath));
                if (System.IO.File.Exists(existingFilePath))
                {
                    System.IO.File.Delete(existingFilePath);
                    _logger.LogInformation("Old image deleted: {FilePath}", existingFilePath);
                }

                postViewModel.Post.FeatureImagePath = await UploadFileToFolder(postViewModel.FeatureImage);
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
            var posts = postQuery.ToList();

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

            if (id == null)
            {
                _logger.LogWarning("Detail: Invalid Post ID = {PostId}", id);
                return NotFound();
            }

            var post = _context.Posts.Include(p => p.Category).Include(p => p.Comments)
                .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                _logger.LogWarning("Detail: Post not found with ID = {PostId}", id);
                return NotFound();
            }

            _logger.LogInformation("Detail: Post loaded successfully with ID = {PostId}", id);
            return View(post);
        }


        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public JsonResult AddComment([FromBody] Comment comment)
        {
            _logger.LogInformation("AddComment called by user = {UserName}", comment?.UserName);

            if (ModelState.IsValid)
            {
                comment.CommentDate = DateTime.Now;
                _context.Comments.Add(comment);
                _context.SaveChanges();

                _logger.LogInformation("Comment added successfully to Post ID = {PostId} by {UserName}",
              comment.PostId, comment.UserName);

                return Json(new
                {
                    userName = comment.UserName,
                    commentDate = comment.CommentDate.ToString("MMMM dd, yyyy"),
                    content = comment.Content
                });
            }

            _logger.LogError("Error while adding comment to Post ID = {PostId}", comment.PostId);
            return Json(new { success = false, message = "Invalid data" });
        }


        private async Task<string> UploadFileToFolder(IFormFile file)
        {
            var inputFileExtension = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid().ToString() + inputFileExtension;
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var imagesFolderPath = Path.Combine(wwwRootPath, "images");

            if (!Directory.Exists(imagesFolderPath))
            {
                Directory.CreateDirectory(imagesFolderPath);
            }

            var filePath = Path.Combine(imagesFolderPath, fileName);

            try
            {
                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                // Log the exception if needed.
                return "Error Uploading Image: " + ex.Message;
            }

            return "/images/" + fileName;
        }
    }
}

