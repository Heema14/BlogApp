using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncSyntax.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [DataType(DataType.Date)]
        [ValidateNever]
        public DateTime CommentDate { get; set; } = DateTime.Now;

        [Required]
        public string? Content { get; set; }

        public int PostId { get; set; }

        [ValidateNever]
        public Post? Post { get; set; }
         
        [Required]
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        [ValidateNever]
        public AppUser? User { get; set; }
    }
}
