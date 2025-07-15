using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SyncSyntax.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "The title is required.")]
    [MaxLength(200, ErrorMessage = "The title cannot exceed 200 characters.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "Please enter the content")]
    [StringLength(2500, ErrorMessage = "Content must not exceed 2500 characters.")]
    public string Content { get; set; }

    [Required(ErrorMessage = "Please enter the description")]
    [StringLength(5000, ErrorMessage = "Description must not exceed 5000 characters.")]
    public string? Description { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(100, ErrorMessage = "Author's name cannot exceed 100 characters.")]
    public DateTime? UpdatedAt { get; set; } = null;
    public int Views { get; set; } = 0;
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string? UserImageUrl { get; set; }
    public string? Tags { get; set; }
    public bool IsPublished { get; set; } = false;
    public int LikesCount { get; set; } = 0;

    [ValidateNever]
    public string? FeatureImagePath { get; set; }

    [DataType(DataType.Date)]
    public DateTime PublishedDate { get; set; } = DateTime.Now;

    [ForeignKey("Category")]
    public int CategoryId { get; set; }

    [ValidateNever]
    public Category Category { get; set; }

    [ValidateNever]
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> PostLikes { get; set; }

}
