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
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedExtension = { ".jpg", ".jpeg", ".png" };

        public PostController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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
        [RequestSizeLimit(10485760)]
        public async Task<IActionResult> Create(PostViewModel postViewModel)
        {
            if (ModelState.IsValid)
            {
                var inputFileExtension = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
                bool isAllowed = _allowedExtension.Contains(inputFileExtension);
                if (!isAllowed)
                {
                    ModelState.AddModelError("Image", "Invalid image format. Allowed formats are .jpg, .jpeg, .png");
                    return View(postViewModel);
                }
                postViewModel.Post.FeatureImagePath = await UploadFileToFolder(postViewModel.FeatureImage);
                _context.Posts.Add(postViewModel.Post);
                _context.SaveChanges();

                return RedirectToAction("Index");
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
            if (!ModelState.IsValid)
            {
                return View(postViewModel);
            }

            var postFromDb = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(
                p => p.Id == postViewModel.Post.Id);

            if (postFromDb == null)
            {
                return NotFound();
            }

            if (postViewModel.FeatureImage != null)
            {
                var inputFileExtension = Path.GetExtension(postViewModel.FeatureImage.FileName).ToLower();
                bool isAllowed = _allowedExtension.Contains(inputFileExtension);
                if (!isAllowed)
                {
                    ModelState.AddModelError("Image", "Invalid image format. Allowed formats are .jpg, .jpeg, .png");
                    return View(postViewModel);
                }

                var existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "images",
                    Path.GetFileName(postFromDb.FeatureImagePath));
                if (System.IO.File.Exists(existingFilePath))
                {
                    System.IO.File.Delete(existingFilePath);
                }
                postViewModel.Post.FeatureImagePath = await UploadFileToFolder(postViewModel.FeatureImage);
            }
            else
            {
                postViewModel.Post.FeatureImagePath = postFromDb.FeatureImagePath;
            }

            _context.Posts.Update(postViewModel.Post);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(int? categoryId)
        {
            var postQuery = _context.Posts.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue)
            {
                postQuery = postQuery.Where(p => p.CategoryId == categoryId);
            }
            var posts = postQuery.ToList();

            ViewData["Categories"] = _context.Categories.ToList();

            return View(posts);
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var postFromDb = await _context.Posts.FindAsync(id);
            if (postFromDb == null)
            {
                return NotFound();
            }
            return View(postFromDb);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            if (id < 0)
            {
                return BadRequest();
            }

            var postFromDb = await _context.Posts.FindAsync(id);
            if (postFromDb == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(postFromDb.FeatureImagePath))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", Path.GetFileName(postFromDb.FeatureImagePath));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }
            _context.Posts.Remove(postFromDb);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public IActionResult Detail(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = _context.Posts.Include(p => p.Category).Include(p => p.Comments)
                .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }
            return View(post);

        }


        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public JsonResult AddComment([FromBody] Comment comment)
        {
            if (ModelState.IsValid)
            {
                comment.CommentDate = DateTime.Now;
                _context.Comments.Add(comment);
                _context.SaveChanges();

                return Json(new
                {
                    userName = comment.UserName,
                    commentDate = comment.CommentDate.ToString("MMMM dd, yyyy"),
                    content = comment.Content
                });
            }

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

