using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SyncSyntax.Models;
using SyncSyntax.Models.ViewModels;

namespace SyncSyntax.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
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
                };

                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    if (model.ProfilePicture.Length > 2 * 1024 * 1024)
                    {
                        _logger.LogWarning("User attempted to upload a profile picture larger than 2MB.");
                        ModelState.AddModelError("ProfilePicture", "File size must be less than 2MB.");
                        return View(model);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                    {
                        _logger.LogWarning("User attempted to upload an unsupported file type: {Extension}", ext);
                        ModelState.AddModelError("ProfilePicture", "Only image files (.jpg, .jpeg, .png) are allowed.");
                        return View(model);
                    }

                    using var ms = new MemoryStream();
                    await model.ProfilePicture.CopyToAsync(ms);
                    user.ProfilePicture = ms.ToArray();

                    _logger.LogInformation("Profile picture uploaded successfully for user: {Email}", model.Email);
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User account created successfully for {Email}", model.Email);

                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        _logger.LogInformation("Default 'User' role created.");
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                    }

                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("User {Email} assigned to 'User' role.", model.Email);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User {Email} signed in after registration.", model.Email);

                    return RedirectToAction("Index", "Post");
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

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("Sign-in failed: no user found with email {Email}.", model.Email);
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(model);
            }
            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Sign-in failed: incorrect password for email {Email}.", model.Email);
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(model);
            }

            _logger.LogInformation("User {Email} signed in successfully.", model.Email);
            return RedirectToAction("Index", "Post");
        }


        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(SignIn));

            var model = new EditProfileViewModel
            {
                Email = user.Email,
                Major = user.MajorName,
                nameUser = user.UserName,
                ProfilePicture = user.ProfilePicture
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
                model.ProfilePicture = user.ProfilePicture;
                return View(model);
            }

            // Update username if changed
            if (user.UserName != model.nameUser && !string.IsNullOrWhiteSpace(model.nameUser))
            {
                var existingUser = await _userManager.FindByNameAsync(model.nameUser);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    _logger.LogWarning("EditProfile: Username {Username} is already taken.", model.nameUser);
                    ModelState.AddModelError(nameof(model.nameUser), "This username is already taken.");
                    model.ProfilePicture = user.ProfilePicture;
                    return View(model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.nameUser);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        _logger.LogWarning("EditProfile: Failed to update username for {Email} - {Error}", user.Email, error.Description);
                        ModelState.AddModelError(nameof(model.nameUser), error.Description);
                    }
                    model.ProfilePicture = user.ProfilePicture;
                    return View(model);
                }
                _logger.LogInformation("EditProfile: Username updated successfully for user {Email}.", user.Email);
            }

            // Update major
            if (user.MajorName != model.Major)
            {
                user.MajorName = model.Major;
                _logger.LogInformation("EditProfile: Major updated for user {Email}.", user.Email);
            }

            // Update profile picture if provided
            if (model.NewProfilePicture != null && model.NewProfilePicture.Length > 0)
            {
                if (model.NewProfilePicture.Length > 2 * 1024 * 1024)
                {
                    _logger.LogWarning("EditProfile: Image too large for user {Email}.", user.Email);
                    ModelState.AddModelError("NewProfilePicture", "Image size cannot exceed 2MB.");
                    model.ProfilePicture = user.ProfilePicture;
                    return View(model);
                }

                using var ms = new MemoryStream();
                await model.NewProfilePicture.CopyToAsync(ms);
                user.ProfilePicture = ms.ToArray();
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
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("EditProfile: User {Email} updated and re-signed in.", user.Email);

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Index", "Post");
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

            return RedirectToAction("Index", "Post");
        }

    }
}
