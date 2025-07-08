using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models.ViewModels
{
    public class SignUpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The major must be (50) length and min length (2) characters."), MinLength(2)]
        public string? MajorName { get; set; }

        public IFormFile? ProfilePicture { get; set; }
    }
}
