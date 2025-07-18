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

            AppUser admin = await CreateUserIfNotExist(userManager, "admin@admin.com", "Admin123/", "admin_admincom", "Admin", "User", Gender.Male, new DateTime(1980, 1, 1), "123456789", "Admin", "This is the admin's bio.");
            AppUser contentCreator = await CreateUserIfNotExist(userManager, "creator@blog.com", "Creator123/", "creator_blogcom", "Content", "Creator", Gender.Female, new DateTime(1990, 6, 15), "987654321", "ContentCreator", "Content creator bio.", "/images/a4.jpg");
            AppUser newContentCreator = await CreateUserIfNotExist(userManager, "newcreator@blog.com", "NewCreator123/", "newcreator_blogcom", "New", "Creator", Gender.Male, new DateTime(1995, 4, 22), "555123456", "ContentCreator", "New content creator bio.", "/images/p03.jpg");

            var adminId = admin?.Id;
            var contentCreatorId = contentCreator?.Id;
            var newContentCreatorId = newContentCreator?.Id;

            if (!context.Posts.Any()) 
            {
                context.Posts.AddRange(
                    new Post
                    {
                        Title = "ContentCreator's First Post",
                        Content = "This is the content of the first post by ContentCreator.",
                        //Description = "This is the description of the post created by the ContentCreator.",
                        FeatureImagePath = "/images/p02.jpg",
                        CreatedAt = new DateTime(2023, 7, 12),
                        UserName = contentCreator?.UserName,
                        UserImageUrl = "/images/p02.jpg",
                        IsPublished = true,
                        PublishedDate = new DateTime(2023, 7, 12),
                        CategoryId = 2,
                        UserId = contentCreatorId
                    },
                    new Post
                    {
                        Title = "NewContentCreator's First Post",
                        Content = "This is the content of the first post by NewContentCreator.",
                        //Description = "This is the description of the post created by NewContentCreator.",
                        FeatureImagePath = "/images/p03.jpg",
                        CreatedAt = new DateTime(2023, 7, 12),
                        UserName = newContentCreator?.UserName,
                        UserImageUrl = "/images/p03.jpg",
                        IsPublished = true,
                        PublishedDate = new DateTime(2023, 7, 12),
                        CategoryId = 3,
                        UserId = newContentCreatorId
                    }
                );

                try
                {
                    await context.SaveChangesAsync(); 
                    logger.LogInformation("Posts added to the database successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error saving posts to database: {ex.Message}"); 
                }
            }
            else
            {
                logger.LogInformation("Posts already exist in the database. Skipping post creation.");
            }
        }

        private static async Task<AppUser> CreateUserIfNotExist(UserManager<AppUser> userManager, string email, string password, string userName, string firstName, string lastName, Gender gender, DateTime dateOfBirth, string phoneNumber, string roleName, string bio = "", string profilePicture = "/images/default-profile.jpg")
        {
            var user = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (user == null)
            {
                var newUser = new AppUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    Gender = gender,
                    DateOfBirth = dateOfBirth,
                    PhoneNumber = phoneNumber,
                    ProfilePicture = profilePicture,
                    Bio = bio 
                };

                var createUser = await userManager.CreateAsync(newUser, password).ConfigureAwait(false);
                if (createUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, roleName).ConfigureAwait(false);
                }
                return newUser;
            }
            return user;
        }



    }
}
