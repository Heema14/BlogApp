using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "User name is required.")]
        [MaxLength(100, ErrorMessage = "User name cannot exceed 100 characters.")]
        public string UserName { get; set; }

        [DataType(DataType.Date)]
        [ValidateNever]
        public DateTime CommentDate { get; set; } = DateTime.Now;

        [Required]
        public string Content { get; set; }

        public int PostId { get; set; }

        [ValidateNever]
        public Post Post { get; set; }
    }
}
