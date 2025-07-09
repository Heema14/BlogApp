using Microsoft.AspNetCore.Identity;

namespace SyncSyntax.Models
{
    public class AppUser : IdentityUser
    {
        public string? MajorName { get; set; }

        public string? ProfilePicture { get; set; }
    }
}
