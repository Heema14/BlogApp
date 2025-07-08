using Microsoft.AspNetCore.Identity;

namespace SyncSyntax.Models
{
    public class AppUser : IdentityUser
    {
        public string? MajorName { get; set; }

        public byte[]? ProfilePicture { get; set; }
    }
}
