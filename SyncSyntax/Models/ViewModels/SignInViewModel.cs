using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models.ViewModels
{
    public class SignInViewModel
    {
        [Required(ErrorMessage = "Email or Username is required.")]
        [Display(Name = "Email or Username")]
        public string EmailOrUsername { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }

}
