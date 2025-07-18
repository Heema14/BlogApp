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
                    UserName = "admin_admincom",
                    Email = EmailAdmin,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    Gender = Gender.Male,
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
                    UserName = "creator_blogcom",
                    Email = ContentCreatorEmail,
                    EmailConfirmed = true,
                    FirstName = "Content",
                    LastName = "Creator",
                    Gender = Gender.Female,
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
            string newContentCreatorEmail = "newcreator@blog.com";
            string newContentCreatorPassword = "NewCreator123/";
            var newContentCreator = await userManager.FindByEmailAsync(newContentCreatorEmail).ConfigureAwait(false);

            if (newContentCreator == null)
            {
                var newContentCreatorUser = new AppUser
                {
                    UserName = "newcreator_blogcom",
                    Email = newContentCreatorEmail,
                    EmailConfirmed = true,
                    FirstName = "New",
                    LastName = "Creator",
                    Gender = Gender.Male,
                    DateOfBirth = new DateTime(1995, 4, 22),
                    PhoneNumber = "555123456",
                    ProfilePicture = "/images/a3.jpg"
                };

                var createNewContentCreator = await userManager.CreateAsync(newContentCreatorUser, newContentCreatorPassword).ConfigureAwait(false);
                if (createNewContentCreator.Succeeded)
                {
                    await userManager.AddToRoleAsync(newContentCreatorUser, "ContentCreator").ConfigureAwait(false);
                    logger.LogInformation("New ContentCreator user created successfully.");
                }
                else
                {
                    logger.LogError("Failed to create New ContentCreator user: " + string.Join(", ", createNewContentCreator.Errors.Select(e => e.Description)));
                }
            }
        }

    }
}
