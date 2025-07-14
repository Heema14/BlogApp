using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models
{
    public class AppUser : IdentityUser
    {
        public string? MajorName { get; set; }

        //public string? MajorName => $"{FirstName} {LastName}";

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

        //// إضافة خاصية FullName
        //public string FullName => $"{FirstName} {LastName}";

        //// إضافة حقل الصورة
        //public string? ProfilePictureUrl { get; set; } // رابط الصورة

        public ICollection<PostLike> PostLikes { get; set; }
    }
}
