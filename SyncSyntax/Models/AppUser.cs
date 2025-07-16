using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models
{
    public class AppUser : IdentityUser
    {
        public string? MajorName { get; set; }

        public string? ProfilePicture { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; } // Male, Female, Other

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PostLike> PostLikes { get; set; }

        // العلاقة بين المستخدمين الذين يتابعهم هذا المستخدم
        public ICollection<Following> FollowedUsers { get; set; }

        // العلاقة بين المتابعين لهذا المستخدم
        public ICollection<Following> Followers { get; set; }


    }
}
