using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models.ViewModels
{
    public class PostViewModel
    {
        public Post Post { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> Categories { get; set; }

        public IFormFile? FeatureImage { get; set; }
    }
}
