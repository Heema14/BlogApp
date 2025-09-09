using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Data;
using System;
using System.Collections.Generic;
using System.Linq;

[Area("Admin")]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    { 
        var allTags = _context.Posts
                      .Where(p => !string.IsNullOrEmpty(p.Tags))
                      .Select(p => p.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                      .ToList();
        var totalTags = allTags.SelectMany(t => t).Select(t => t.Trim()).Distinct().Count();

        var model = new AdminDashboardViewModel
        {
            TotalUsers = _context.Users.Count(),
            TotalPosts = _context.Posts.Count(),
            TotalCategories = _context.Categories.Count(),
            TotalTags = totalTags,
            TotalLikes = _context.Posts.Sum(p => p.LikesCount),
            TotalComments = _context.Posts.Sum(p => p.Comments.Count),
            TotalViews = _context.Posts.Sum(p => p.Views)
        };


         var allDays = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-6 + i))
            .ToList();

         var chartLabels = allDays.Select(d => d.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture)).ToList();

         var chartData = allDays.Select(d => 0).ToList();

         using (var connection = _context.Database.GetDbConnection())
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "GetNewUsersLast7Days";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var day = ((DateTime)reader["UserDate"]).ToString("ddd", System.Globalization.CultureInfo.InvariantCulture);
                        var index = chartLabels.IndexOf(day);
                        if (index >= 0)
                        {
                            chartData[index] = (int)reader["TotalUsers"];
                        }
                    }
                }
            }
        }

         ViewBag.ChartLabels = chartLabels;
        ViewBag.ChartData = chartData;


        return View(model);
    }

    public IActionResult Categories()
    {
        return View();
    }
}
