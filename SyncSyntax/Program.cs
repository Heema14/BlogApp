using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.Hubs;
using SyncSyntax.Models.IServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5 MB
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;              // مش لازم رقم
    options.Password.RequireLowercase = true;          // لازم حرف صغير
    options.Password.RequireUppercase = true;          // لازم حرف كبير
    options.Password.RequireNonAlphanumeric = false;    // مش لازم رمز
    options.Password.RequiredLength = 6;                // أقل طول = 6
    options.Password.RequiredUniqueChars = 2;

    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-_@ ";
    options.User.RequireUniqueEmail = true;

    // Lockout options
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = new TimeSpan(0, 5, 0);
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/SignIn";  // Redirect path for unauthorized users 🔐
    options.AccessDeniedPath = "/Auth/AccessDenied"; // ⛔
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // ⏰
    options.SlidingExpiration = true;
});


builder.Services.AddScoped<IUploadFileService, UploadFileService>();

builder.Services.AddSignalR();

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        await DatabaseSeeder.Initialize(scope.ServiceProvider);
    }
}
catch (Exception)
{
    Console.WriteLine("An error occurred while seeding the database.");
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();


app.MapHub<CommentHub>("/commentHub");
app.MapHub<PostLikeHub>("/postHub");


app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "admin/{controller=Home}/{action=Index}/{id?}"
    );

app.MapAreaControllerRoute(
    name: "contentcreator",
    areaName: "ContentCreator",
    pattern: "ContentCreator/{controller=Following}/{action=FollowingPosts}/{id?}"
    );

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
    );


app.Run();
