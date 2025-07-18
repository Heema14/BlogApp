using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SyncSyntax.Data;
using SyncSyntax.Models;
using SyncSyntax.Models.IServices;
using SyncSyntax.Models.ViewModels;

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
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var username = model.Email.Split('@')[0]; //take user name from email

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
                        _logger.LogWarning("User attempted to upload a profile picture larger than {MaxMB}MB.", maxSizeMb);
                        ModelState.AddModelError("ProfilePicture", $"File size must be less than {maxSizeMb}MB.");
                        return View(model);
                    }

                    var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                    {
                        _logger.LogWarning("User attempted to upload an unsupported file type: {Extension}", ext);
                        ModelState.AddModelError("ProfilePicture", $"Only these formats are allowed: {string.Join(", ", allowedExtensions)}");
                        return View(model);
                    }

                    user.ProfilePicture = await _uploadFile.UploadFileToFolderAsync(model.ProfilePicture);

                    _logger.LogInformation("Profile picture saved at path: {Path} for user: {Email}", user.ProfilePicture, model.Email);
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User account created successfully for {Email}", model.Email);

                    if (!await _roleManager.RoleExistsAsync("ContentCreator"))
                    {
                        _logger.LogInformation("Default 'ContentCreator' role created.");
                        await _roleManager.CreateAsync(new IdentityRole("ContentCreator"));
                    }

                    await _userManager.AddToRoleAsync(user, "ContentCreator");
                    _logger.LogInformation("User {Email} assigned to 'ContentCreator' role.", model.Email);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User {Email} signed in after registration.", model.Email);

                    // Add a notification here
                    var notification = new Notification
                    {
                        UserId = user.Id,  // Add the user ID of the one who signed up
                        Message = $"Welcome {user.UserName}! Your account has been successfully created.",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("FollowingPosts", "Following", new { area = "ContentCreator" });
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("Error creating user {Email}: {Error}", model.Email, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Sign-in attempt failed due to invalid model state.");
                return View(model);
            }

            // UserNmae or Email
            AppUser? user = null;

            user = await _userManager.FindByEmailAsync(model.EmailOrUsername);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(model.EmailOrUsername);
            }

            if (user == null)
            {
                _logger.LogWarning("Sign-in failed: no user found with email or username {Input}.", model.EmailOrUsername);
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(model);
            }

            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Sign-in failed: incorrect password for user {Input}.", model.EmailOrUsername);
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(model);
            }

            _logger.LogInformation("User {Input} signed in successfully.", model.EmailOrUsername);
            return RedirectToAction("FollowingPosts", "Following", new { area = "ContentCreator" });
        }


        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(SignIn));

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Gender = user.Gender,
                Email = user.Email,
                Major = user.MajorName,
                NameUser = user.UserName,
                DateOfBirth = user.DateOfBirth,
                PhoneNumber = user.PhoneNumber,
                ProfilePicturePath = user.ProfilePicture
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("EditProfile: No user found for current session.");
                return RedirectToAction(nameof(SignIn));
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("EditProfile: Invalid model state for user {Email}.", user.Email);
                model.ProfilePicturePath = user.ProfilePicture;
                return View(model);
            }

            // Update username if changed
            if (user.UserName != model.NameUser && !string.IsNullOrWhiteSpace(model.NameUser))
            {
                var existingUser = await _userManager.FindByNameAsync(model.NameUser);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    _logger.LogWarning("EditProfile: Username {Username} is already taken.", model.NameUser);
                    ModelState.AddModelError(nameof(model.NameUser), "This username is already taken.");
                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.NameUser);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        _logger.LogWarning("EditProfile: Failed to update username for {Email} - {Error}", user.Email, error.Description);
                        ModelState.AddModelError(nameof(model.NameUser), error.Description);
                    }
                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }

                _logger.LogInformation("EditProfile: Username updated successfully for user {Email}.", user.Email);
            }

            if (user.MajorName != model.Major)
            {
                user.MajorName = model.Major;
                _logger.LogInformation("EditProfile: Major updated for user {Email}.", user.Email);
            }
            if (user.FirstName != model.FirstName)
            {
                user.FirstName = model.FirstName;
                _logger.LogInformation("EditProfile: FirstName updated for user {Email}.", user.Email);
            }
            if (user.LastName != model.LastName)
            {
                user.LastName = model.LastName;
                _logger.LogInformation("EditProfile: LastName updated for user {Email}.", user.Email);
            }
            if (user.DateOfBirth != model.DateOfBirth)
            {
                user.DateOfBirth = model.DateOfBirth;
                _logger.LogInformation("EditProfile: DateOfBirth updated for user {Email}.", user.Email);
            }
            if (user.Gender != model.Gender)
            {
                user.Gender = model.Gender;
                _logger.LogInformation("EditProfile: Gender updated for user {Email}.", user.Email);
            }
            if (user.PhoneNumber != model.PhoneNumber)
            {
                user.PhoneNumber = model.PhoneNumber;
                _logger.LogInformation("EditProfile: PhoneNumber updated for user {Email}.", user.Email);
            }

            // Update profile picture if provided
            if (model.NewProfilePicture != null && model.NewProfilePicture.Length > 0)
            {
                var allowedExtensions = _config.GetSection("uploading:allowedFileExtension").Get<List<string>>();
                var maxSizeMb = _config.GetValue<int>("uploading:allowedFileSize");
                var maxSizeBytes = maxSizeMb * 1024 * 1024;

                var ext = Path.GetExtension(model.NewProfilePicture.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("EditProfile: Unsupported file type: {Extension}", ext);
                    ModelState.AddModelError("NewProfilePicture", $"Only formats allowed: {string.Join(", ", allowedExtensions)}");
                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }

                // delete old image
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", Path.GetFileName(user.ProfilePicture));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                        _logger.LogInformation("EditProfile: Old profile picture deleted at {Path}", oldFilePath);
                    }
                }

                if (model.NewProfilePicture.Length > maxSizeBytes)
                {
                    _logger.LogWarning("EditProfile: Image too large for user {Email}.", user.Email);
                    ModelState.AddModelError("NewProfilePicture", $"Image size cannot exceed {maxSizeMb}MB.");
                    model.ProfilePicturePath = user.ProfilePicture;
                    return View(model);
                }

                user.ProfilePicture = await _uploadFile.UploadFileToFolderAsync(model.NewProfilePicture);
                _logger.LogInformation("EditProfile: Profile picture updated for user {Email}.", user.Email);
            }

            // Apply updates
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogError("EditProfile: Failed to update user {Email} - {Error}", user.Email, error.Description);
                    ModelState.AddModelError("", error.Description);
                }
                model.ProfilePicturePath = user.ProfilePicture;
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("EditProfile: User {Email} updated and re-signed in.", user.Email);

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("FollowingPosts", "Following", new { area = "ContentCreator" });

        }


        public IActionResult AccessDenied()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> SignOut()
        {
            var user = await _userManager.GetUserAsync(User);
            string email = user?.Email ?? "Unknown";

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {Email} signed out successfully.", email);

            return RedirectToAction("Index", "Home");
        }

    }
}
