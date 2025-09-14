using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
[Area("Admin")]

public class UsersController : Controller
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var usersWithCounts = _context.Users
        .Select(u => new UserWithPostsCountViewModel
        {
            User = u,
            PostsCount = _context.Posts.Count(p => p.UserId == u.Id)
        }).ToList();

        return View(usersWithCounts);
    }
}
