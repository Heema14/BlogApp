// ... نفس using statements ...

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SyncSyntax.Data;
using SyncSyntax.Models.IServices;
using SyncSyntax.Models.ViewModels;
using SyncSyntax.Models;
using System.Linq;

namespace SyncSyntax.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly IUploadFileService _uploadFile;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _context;

        public AuthController(SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AuthController> logger,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration config,
            IUploadFileService uploadFile,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _config = config;
            _uploadFile = uploadFile;
            _context = context;
        }

        [HttpGet]
        public IActionResult SignUp() => View();

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var username = model.Email.Split('@')[0];

                var user = new AppUser
                {
                    Email = model.Email,
                    UserName = username,
                    MajorName = model.MajorName,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Gender = model.Gender,
                    DateOfBirth = model.DateOfBirth,
                };

            
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    var allowedExtensions = _config.GetSection("uploading:allowedFileExtension").Get<List<string>>();
                    var maxSizeMb = _config.GetValue<int>("uploading:allowedFileSize");
                    var maxSizeBytes = maxSizeMb * 1024 * 1024;

                    if (model.ProfilePicture.Length > maxSizeBytes)
                    {
                        _logger.LogWarning("Large profile picture");
                        ModelState.AddModelError("ProfilePicture", $"File size must be less than {maxSizeMb}MB.");
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                    {
                        _logger.LogWarning("Invalid profile picture extension");
                        ModelState.AddModelError("ProfilePicture", $"Only these formats are allowed: {string.Join(", ", allowedExtensions)}");
                        return View(model);
                    }

                    user.ProfilePicture = await _uploadFile.UploadFileToFolderAsync(model.ProfilePicture);
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    
                    if (!await _roleManager.RoleExistsAsync("ContentCreator"))
                        await _roleManager.CreateAsync(new IdentityRole("ContentCreator"));

                    await _userManager.AddToRoleAsync(user, "ContentCreator");
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    _context.Notifications.Add(new Notification
                    {
                        UserId = user.Id,
                        Message = $"Welcome {user.UserName}! Your account has been successfully created.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Account created successfully! Welcome 🎉";
                     
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else if (await _userManager.IsInRoleAsync(user, "ContentCreator"))
                    {
                        return RedirectToAction("FollowingPosts", "Following", new { area = "ContentCreator" });
                    }

                   
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("Error creating user: " + error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult SignIn() => View();

        [HttpPost]

        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            AppUser? user = await _userManager.FindByEmailAsync(model.EmailOrUsername)
                          ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                TempData["Error"] = "User not found.";
                return View(model);
            }

            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            if (!signInResult.Succeeded)
            {
                TempData["Error"] = "Incorrect password.";
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(model);
            }

            TempData["Success"] = $"Welcome back, {user.UserName}!";

          
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            else if (await _userManager.IsInRoleAsync(user, "ContentCreator"))
            {
                return RedirectToAction("FollowingPosts", "Following", new { area = "ContentCreator" });
            }
 
            return RedirectToAction("Index", "Home", new { area = "" });
        }


        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(SignIn));

            return View(new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Gender = user.Gender,
                Email = user.Email,
                Major = user.MajorName,
                Bio = user.Bio,
                NameUser = user.UserName,
                DateOfBirth = user.DateOfBirth,
                PhoneNumber = user.PhoneNumber,
                ProfilePicturePath = user.ProfilePicture
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(SignIn));

            if (!ModelState.IsValid)
            {
                model.ProfilePicturePath = user.ProfilePicture;
                return View(model);
            }

            if (user.UserName != model.NameUser && !string.IsNullOrWhiteSpace(model.NameUser))
            {
                var existingUser = await _userManager.FindByNameAsync(model.NameUser);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.NameUser), "This username is already taken.");
                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.NameUser);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                        ModelState.AddModelError(nameof(model.NameUser), error.Description);

                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.MajorName = model.Major;
            user.Gender = model.Gender;
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;
            user.Bio = model.Bio;

            if (model.NewProfilePicture != null && model.NewProfilePicture.Length > 0)
            {
                var allowedExtensions = _config.GetSection("uploading:allowedFileExtension").Get<List<string>>();
                var maxSizeMb = _config.GetValue<int>("uploading:allowedFileSize");
                var maxSizeBytes = maxSizeMb * 1024 * 1024;

                var ext = Path.GetExtension(model.NewProfilePicture.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("NewProfilePicture", $"Only formats allowed: {string.Join(", ", allowedExtensions)}");
                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }

                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", Path.GetFileName(user.ProfilePicture));
                    if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                }

                if (model.NewProfilePicture.Length > maxSizeBytes)
                {
                    ModelState.AddModelError("NewProfilePicture", $"Image size cannot exceed {maxSizeMb}MB.");
                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }

                user.ProfilePicture = await _uploadFile.UploadFileToFolderAsync(model.NewProfilePicture);
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.ProfilePicturePath = user.ProfilePicture;
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("FollowingPosts", "Following", new { area = "ContentCreator" });
        }

        public IActionResult AccessDenied() => View();

        [HttpGet]
        public async Task<IActionResult> SignOut()
        {
            var user = await _userManager.GetUserAsync(User);
            await _signInManager.SignOutAsync();
            TempData["Info"] = $"Goodbye {user?.UserName ?? "user"}!";
            return RedirectToAction("Index", "Home");
        }
    }
}
