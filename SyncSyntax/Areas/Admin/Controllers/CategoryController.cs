using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;

namespace SyncSyntax.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(AppDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [Authorize]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            return View(categories);
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category category)
        {
            _logger.LogInformation("Create(Category) via Fetch called.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model: {@ModelState}", ModelState);
                return BadRequest(new { success = false, message = "Invalid data." });
            }

            try
            {
                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category created successfully: {@Category}", category);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category.");
                return StatusCode(500, new { success = false, message = "Server error." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            return PartialView("~/Views/Shared/_EditPartial.cshtml", category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromForm] Category category)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data." });

            try
            {
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return StatusCode(500, new { success = false, message = "Server error." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            _logger.LogInformation("DeleteConfirm(Category) called with invalid model state: {@CategoryID}", id);

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return Json(new { success = false, message = "Category not found." });

            _context.Categories.Remove(category);
            _logger.LogInformation("DeleteConfirm(Category) called with Remove: {@CategoryID}", id);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

    }
}
