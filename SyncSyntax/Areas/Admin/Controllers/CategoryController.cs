using Microsoft.AspNetCore.Mvc;
 
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using SyncSyntax.Models;

[Area("Admin")]
public class CategoryController : Controller
{
    private readonly AppDbContext _context;
    public CategoryController(AppDbContext context) => _context = context;

    public IActionResult Index() => View();

    // Get all categories with posts count
    public async Task<JsonResult> List()
    {
        var categories = await _context.Categories
            .Select(c => new {
                c.Id,
                c.Name,
                c.Description,
                PostsCount = _context.Posts.Count(p => p.CategoryId == c.Id)
            }).ToListAsync();

        return Json(categories);
    }

    // Add/Edit category
    [HttpPost]
    public async Task<JsonResult> Save([FromForm] Category category)
    {
        if (category.Id == 0)
            _context.Categories.Add(category);
        else
        {
            var existing = await _context.Categories.FindAsync(category.Id);
            if (existing != null)
            {
                existing.Name = category.Name;
                existing.Description = category.Description;
            }
        }
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    // Delete category
    [HttpPost]
    public async Task<JsonResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
        return Json(new { success = true });
    }
}
