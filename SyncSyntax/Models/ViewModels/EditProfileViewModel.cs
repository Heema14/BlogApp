using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [MinLength(3)]
        public string? nameUser { get; set; }

        [MinLength(2)]
        public string? Major { get; set; }

        public byte[]? ProfilePicture { get; set; } // just to show image (img src) 

        // New image
        public IFormFile? NewProfilePicture { get; set; } // to edit image in page <input type="file"> 
    }
}
