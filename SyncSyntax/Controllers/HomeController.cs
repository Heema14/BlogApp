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
            var posts = _context.Posts;
            _logger.LogInformation("Show Index Page!!");
            return View(posts);
           
        }
    }
}
