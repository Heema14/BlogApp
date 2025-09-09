using Microsoft.AspNetCore.Mvc;
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
        var users = _context.Users.ToList();  
        return View(users);
    }
}
