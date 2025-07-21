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
        public Gender Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Phone]
        [StringLength (15)]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastSeen { get; set; }  

        public ICollection<PostLike> PostLikes { get; set; }

      
        public ICollection<Following> FollowedUsers { get; set; }

        public string? Bio { get; set; }
        public ICollection<Following> Followers { get; set; }
        public ICollection<Notification> Notifications { get; set; }
       
    }
}
