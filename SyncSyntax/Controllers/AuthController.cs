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

        public AuthController(SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
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
                        ModelState.AddModelError("ProfilePicture", "File size must be less than 2MB.");
                        return View(model);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ProfilePicture", "Only image files (.jpg, .jpeg, .png) are allowed.");
                        return View(model);
                    }

                    using var ms = new MemoryStream();
                    await model.ProfilePicture.CopyToAsync(ms);
                    user.ProfilePicture = ms.ToArray();
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("User"))
                        await _roleManager.CreateAsync(new IdentityRole("User"));

                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Post");
                }

                foreach (var error in result.Errors)
                {
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

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password");
                    return View(model);
                }
                var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);

                if (!signInResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password");
                    return View(model);
                }

                return RedirectToAction("Index", "Post");
            }
            return View(model);
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

            if (!ModelState.IsValid)
            {
                model.ProfilePicture = user.ProfilePicture;
                return View(model);
            }

            if (user == null) return RedirectToAction(nameof(SignIn));

            if (user.UserName != model.nameUser && !string.IsNullOrWhiteSpace(model.nameUser))
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.nameUser);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(nameof(model.nameUser), error.Description);
                    }
                    model.ProfilePicture = user.ProfilePicture;
                    return View(model);
                }
            }

            if (user.MajorName != model.Major)
                user.MajorName = model.Major;

            if (model.NewProfilePicture != null && model.NewProfilePicture.Length > 0)
            {
                if (model.NewProfilePicture.Length > 2 * 1024 * 1024) // 2MB
                {
                    ModelState.AddModelError("NewProfilePicture", "Image size cannot exceed 2MB.");
                    model.ProfilePicture = user.ProfilePicture;
                    return View(model);
                }

                using var ms = new MemoryStream();
                await model.NewProfilePicture.CopyToAsync(ms);
                user.ProfilePicture = ms.ToArray();
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

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
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Post");
        }

    }
}
