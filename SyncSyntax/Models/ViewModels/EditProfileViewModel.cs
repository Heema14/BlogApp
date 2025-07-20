using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string FirstName { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string LastName { get; set; }

        [MinLength(3)]
        public string? NameUser { get; set; }

        [MinLength(2)]
        public string? Major { get; set; }

        public string? ProfilePicturePath { get; set; } // image is exist

        public IFormFile? NewProfilePicture { get; set; } // new image

        public Gender Gender { get; set; }

        [MinLength(20), MaxLength(200)]
        public string? Bio { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Phone]
        [StringLength(15)]
        public string? PhoneNumber { get; set; }
    }

}
