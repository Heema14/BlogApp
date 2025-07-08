using Microsoft.AspNetCore.Identity;
using SyncSyntax.Models;

namespace SyncSyntax.Data
{
    public class DatabaseSeeder
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // Seed roles
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed admin user
            if (await _userManager.FindByEmailAsync("admin@admin.com") == null)
            {
                var adminUser = new AppUser { UserName = "admin@admin.com", Email = "admin@admin.com" };
                await _userManager.CreateAsync(adminUser, "Admin@123");
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Seed regular user
            if (await _userManager.FindByEmailAsync("user123@gmail.com") == null)
            {
                var regularUser = new AppUser { UserName = "user123@gmail.com", Email = "user123@gmail.com" };
                await _userManager.CreateAsync(regularUser, "User@123");
                await _userManager.AddToRoleAsync(regularUser, "User");
            }
        }
    }
}
