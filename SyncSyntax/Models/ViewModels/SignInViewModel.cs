using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models.ViewModels
{
    public class SignInViewModel
    {

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
