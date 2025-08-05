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
        public DbSet<Following> Followings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageDeletion> MessageDeletions { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<SavedPost> SavedPosts { get; set; }

        public DbSet<ArchivedMessage> ArchivedMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Following>()
                .HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Following>()
                .HasOne(f => f.FollowedUser)
                .WithMany()
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Post>()
                .HasMany(p => p.PostLikes)
                .WithOne(pl => pl.Post)
                .HasForeignKey(pl => pl.PostId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.User)
                .WithMany(u => u.PostLikes)
                .HasForeignKey(pl => pl.UserId);

            modelBuilder.Entity<Post>()
                        .HasOne(p => p.User)
                        .WithMany()
                        .HasForeignKey(p => p.UserId)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppUser>()
                .Property(u => u.Gender)
                .HasConversion<string>();

            modelBuilder.Entity<Message>()
        .HasOne<AppUser>()
        .WithMany()
        .HasForeignKey(m => m.SenderId)
        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
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

          
  
        }

    }


}


