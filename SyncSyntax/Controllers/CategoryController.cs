using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;

namespace SyncSyntax.Controllers
{
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
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            _logger.LogInformation("Create(Category) called.");

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.Categories.AddAsync(category);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Category created successfully: {@Category}", category);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while creating the category: {@Category}", category);
                    ModelState.AddModelError("", "Unexpected error while saving data.");
                }
            }
            else
            {
                _logger.LogWarning("Create(Category) called with invalid model state: {@ModelState}", ModelState);
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        public async Task<IActionResult> Edit(Category category)
        {
            _logger.LogInformation("Edit(Category) called.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Categories.Update(category);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Category updated successfully: {@Category}", category);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating the category: {@Category}", category);
                    ModelState.AddModelError("", "Unexpected error while updating the data.");
                }
            }
            else
            {
                _logger.LogWarning("Edit(Category) called with invalid model state: {@ModelState}", ModelState);
            }

            return View(category);
        }

        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Delete(int) called with ID = {CategoryId}", id);

            var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id);
            if (category == null)
            {
                _logger.LogWarning("Delete(int): Category not found with ID = {CategoryId}", id);
                return NotFound();
            }
            _logger.LogInformation("Delete(int): Category found. Displaying delete confirmation.");
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            _logger.LogInformation("DeleteConfirm(int) called with ID = {CategoryId}", id);

            var categoryFromDb = await _context.Categories.FindAsync(id);
            if (categoryFromDb == null)
            {
                _logger.LogWarning("DeleteConfirm(int): Category not found with ID = {CategoryId}", id);
                return NotFound();
            }
            _context.Categories.Remove(categoryFromDb);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category deleted successfully with ID = {CategoryId}", id);
            return RedirectToAction(nameof(Index));
        }
    }
}
