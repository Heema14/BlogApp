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

        public string? ProfilePicturePath { get; set; } // to use in <img src="...">

        // New image
        public IFormFile? NewProfilePicture { get; set; } // to edit image in page <input type="file"> 
    }
}
