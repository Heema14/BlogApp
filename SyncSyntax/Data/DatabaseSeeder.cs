using Microsoft.AspNetCore.Identity;
using SyncSyntax.Models;

namespace SyncSyntax.Data
{
    public class DatabaseSeeder
    {
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
            string adminPassword = "Admin@123";
            var admin = await userManager.FindByEmailAsync(EmailAdmin).ConfigureAwait(false);

            string ContentCreatorEmail = "user@user.com";
            string ContentCreatorPass = "User@123";
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
