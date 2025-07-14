using Microsoft.AspNetCore.Identity;
using SyncSyntax.Models;

namespace SyncSyntax.Data
{
    public class DatabaseSeeder
    {
        //private readonly UserManager<AppUser> _userManager;
        //private readonly RoleManager<IdentityRole> _roleManager;

        //public DatabaseSeeder(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        //{
        //    _userManager = userManager;
        //    _roleManager = roleManager;
        //}

        //public async Task SeedAsync()
        //{
        //    // Seed roles
        //    string[] roles = { "Admin", "User" };
        //    foreach (var role in roles)
        //    {
        //        if (!await _roleManager.RoleExistsAsync(role))
        //        {
        //            await _roleManager.CreateAsync(new IdentityRole(role));
        //        }
        //    }

        //    // Seed admin user
        //    if (await _userManager.FindByEmailAsync("admin@admin.com") == null)
        //    {
        //        var adminUser = new AppUser { UserName = "admin@admin.com", Email = "admin@admin.com" };
        //        await _userManager.CreateAsync(adminUser, "Admin@123");
        //        await _userManager.AddToRoleAsync(adminUser, "Admin");
        //    }

        //    // Seed regular user
        //    if (await _userManager.FindByEmailAsync("user123@gmail.com") == null)
        //    {
        //        var regularUser = new AppUser { UserName = "user123@gmail.com", Email = "user123@gmail.com" };
        //        await _userManager.CreateAsync(regularUser, "User@123");
        //        await _userManager.AddToRoleAsync(regularUser, "User");
        //    }
        //}


        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();

            string[] roleNames = { "Admin", "ContentCreator" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false);
                if (!roleExist)
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Role '{roleName}' created successfully.");
                    }
                    else
                    {
                        logger.LogError($"Failed to create role '{roleName}'.");
                    }
                }
                else
                {
                    logger.LogInformation($"Role '{roleName}' already exists.");
                }
            }

            string EmailAdmin = "admin@admin.com";
            string adminPassword = "Admin123/";
            var admin = await userManager.FindByEmailAsync(EmailAdmin).ConfigureAwait(false);

            string ContentCreatorEmail = "creator@blog.com";
            string ContentCreatorPass = "Creator123/";
            var contentCreator = await userManager.FindByEmailAsync(ContentCreatorEmail).ConfigureAwait(false);

            // إنشاء مستخدم "Admin" إذا لم يكن موجودًا
            if (admin == null)
            {
                var adminUser = new AppUser
                {
                    UserName = EmailAdmin,
                    Email = EmailAdmin,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    Gender = "Male",
                    DateOfBirth = new DateTime(1980, 1, 1),
                    PhoneNumber = "123456789"
                };

                var createUser = await userManager.CreateAsync(adminUser, adminPassword).ConfigureAwait(false);
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin").ConfigureAwait(false);
                    logger.LogInformation("Admin user created successfully.");
                }
                else
                {
                    logger.LogError("Failed to create Admin user: " + string.Join(", ", createUser.Errors.Select(e => e.Description)));
                }
            }

            // إنشاء مستخدم "ContentCreator" إذا لم يكن موجودًا
            if (contentCreator == null)
            {
                var contentCreatorUser = new AppUser
                {
                    UserName = ContentCreatorEmail,
                    Email = ContentCreatorEmail,
                    EmailConfirmed = true,
                    FirstName = "Content",
                    LastName = "Creator",
                    Gender = "Female",
                    DateOfBirth = new DateTime(1990, 6, 15),
                    PhoneNumber = "987654321",
                    ProfilePicture = "/images/a2.jpg"
                };

                var createContentCreator = await userManager.CreateAsync(contentCreatorUser, ContentCreatorPass).ConfigureAwait(false);
                if (createContentCreator.Succeeded)
                {
                    await userManager.AddToRoleAsync(contentCreatorUser, "ContentCreator").ConfigureAwait(false);
                    logger.LogInformation("ContentCreator user created successfully.");
                }
                else
                {
                    logger.LogError("Failed to create ContentCreator user: " + string.Join(", ", createContentCreator.Errors.Select(e => e.Description)));
                }
            }
        }

    }
}
