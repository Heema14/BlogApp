using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;

namespace SyncSyntax.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;

        }
        public IActionResult Index()
        {
            var posts = _context.Posts.Where(p => p.IsPublished).ToList(); //Just Published
            _logger.LogInformation("Show Posts is Published");
            return View(posts);
        }

    }
}
