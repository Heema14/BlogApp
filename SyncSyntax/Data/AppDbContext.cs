using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Models;

namespace SyncSyntax.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Post>()
           .HasMany(p => p.PostLikes)
           .WithOne(pl => pl.Post)
           .HasForeignKey(pl => pl.PostId)
           .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.User)
                .WithMany(u => u.PostLikes)
                .HasForeignKey(pl => pl.UserId);

            // Call the Seed method to populate initial data
            Seed(modelBuilder);
        }

        public static void Seed(ModelBuilder modelBuilder)
        {
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Health" },
            new Category { Id = 2, Name = "Technology" },
            new Category { Id = 3, Name = "Programming" },
            new Category { Id = 4, Name = "Fashion" },
            new Category { Id = 5, Name = "Design" }
        );

            // Seed Posts with static values for PublishedDate
            modelBuilder.Entity<Post>().HasData(
                  new Post
                  {
                      Id = 1,
                      Title = "Post One",
                      Content = "Content of the first post",
                      Description = "This is a description for post one.",
                      FeatureImagePath = "/images/p03.jpg",
                      CreatedAt = new DateTime(2023, 7, 12),
                      UserName = "user1",
                      UserImageUrl = "/images/p09.jpg",
                      IsPublished = true,
                      PublishedDate = new DateTime(2023, 7, 12),
                      CategoryId = 1
                  },
    new Post
    {
        Id = 2,
        Title = "Post Two",
        Content = "Content of the second post",
        Description = "This is a description for post two.",
        FeatureImagePath = "/images/p01.jpg", 
        CreatedAt = new DateTime(2023, 7, 12),
        UserName = "user2",
        UserImageUrl = "/images/p08.jpg",
        IsPublished = true,
        PublishedDate = new DateTime(2023, 7, 12),
        CategoryId = 2,

    }
           
            );
        }

    }


}


